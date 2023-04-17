using System;
using System.Threading.Tasks;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.Helpers;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        #region Setup
        private readonly RoleManager<Role> roleManager;
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IAccountRepository accountRepository;
        private readonly IBetRepository betRepository;
        private readonly IMessageRepository messageRepository;
        public AccountViewModel viewModel;

        public AccountController(RoleManager<Role> roleManager, UserManager<User> userManager,
                                SignInManager<User> signInManager, IAccountRepository accountRepository, IBetRepository betRepository, IMessageRepository messageRepository)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.accountRepository = accountRepository;
            this.betRepository = betRepository;
            this.messageRepository = messageRepository;
            viewModel = new AccountViewModel();
        }
        #endregion

        #region Account Management
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        // This method registers and signs in new user
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel user)
        {
            if (ModelState.IsValid)
            {
                var newUser = new User { UserName = user.UserName, Email = user.Email };
                var result = await userManager.CreateAsync(newUser, user.Password);

                if (result.Succeeded)
                {
                    await AddRole(newUser.Id, Const.ROLE_BASIC); //adds a Basic Role to new User
                    await accountRepository.StartSubscription(Const.PLAN_BASIC, newUser.Id); //starts a basic subscription
                    await signInManager.SignInAsync(newUser, isPersistent: false);
                    return RedirectToAction("Index", "Prediction");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(user);
        }

        public async Task AddRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            await userManager.AddToRoleAsync(user, role);
        }

        public async Task RemoveRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            await userManager.RemoveFromRoleAsync(user, role);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        //This method logs user in
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Prediction", new { TipTypeId = 1 });
                }
                ModelState.AddModelError(string.Empty, "Neuspesno!");
            }
            return View(model);
        }

        //This method logs user out
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        #endregion

        public async Task<IActionResult> CreateViewModel()
        {
            string userId = GetUserId();
            string roleName = GetUserRoleName();

            var userTask = accountRepository.GetUser(userId);
            var roleTask = accountRepository.GetRole(roleName);
            var ticketsTask = betRepository.GetUserTickets(userId);
            var invoicesTask = accountRepository.GetInvoicesByUserId(userId);
            var receivedMessagesTask = accountRepository.GetReceivedMessagesByUserId(userId);
            var sentMessagesTask = accountRepository.GetSentMessagesByUserId(userId);
            var expDateTask = roleName != Const.ROLE_BASIC
                                        ? accountRepository.GetSubscriptionExpDate(userId)
                                        : Task.FromResult(DateTime.Now.AddYears(1));

            await Task.WhenAll(userTask, roleTask, ticketsTask, invoicesTask, receivedMessagesTask, sentMessagesTask, expDateTask);

            viewModel.User = userTask.Result;
            viewModel.Role = roleTask.Result;
            viewModel.Tickets = ticketsTask.Result;
            viewModel.Invoices = invoicesTask.Result;
            viewModel.ReceivedMessages = receivedMessagesTask.Result;
            viewModel.SentMessages = sentMessagesTask.Result;
            viewModel.ExpDate = expDateTask.Result;

            return View("Profile", viewModel);
        }

        [Route("account")]
        [HttpGet]
        public async Task<IActionResult> Profile() => await CreateViewModel();

        #region Checkout

        [Route("checkout")]
        [HttpGet]
        public async Task<IActionResult> Checkout(int planId)
        {
            var clientId = "AYBqN4OuHgNob-xgAlid2aDweFNmAtodoHkYu8BObmrmZu3BfpSV9IUyfUXzIQcKSzAyJqdWTshCeiHe";
            var plan = await accountRepository.GetPlan(planId); // get plan by id
            var checkoutViewModel = new CheckoutViewModel //create model based on plan
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                MonotonousTipsAmount = plan.MonotonousTipsAmount,
                AdventurousTipsAmount = plan.AdventurousTipsAmount,
                Price = plan.Price
            };

            ViewData["ClientId"] = clientId;
            ViewData["PlanId"] = planId;
            return View("Checkout", checkoutViewModel);
        }

        [Route("account/subscription/approved/{OrderId}")]
        public async Task SubscriptionApproved(string orderId, string role, int planId)
        {
            await accountRepository.StartSubscription(planId, GetUserId());
            await accountRepository.CreateInvoice(GetUserId());
            await RemoveRole(GetUserId(), GetUserRoleName());
            await AddRole(GetUserId(), role);
        }

        #endregion

        #region Message

        [Route("contact")]
        public IActionResult Contact() => View(new MessageViewModel() { TipTypeId = 0 });

        [HttpPost]
        public async Task<IActionResult> SendMessage(MessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                var message = new Message()
                {
                    SenderId = GetUserId(),
                    ReceiverId = "9d9d680b-61d3-40c3-8593-802124323775",
                    Subject = model.Subject,
                    Text = model.Text,
                    IsMessageRead = false
                };
                await messageRepository.AddMessage(message);
                return await CreateViewModel();
            }
            return View("Contact", model);
        }
        #endregion
    }
}
