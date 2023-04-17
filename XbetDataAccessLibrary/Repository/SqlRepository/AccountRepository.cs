using DataAccessLibrary.DataAccess;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLibrary.Repository.SqlRepository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly XbetContext db;
        private readonly UserManager<User> userManager;

        public AccountRepository(XbetContext db, UserManager<User> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        //This method returns user that has id equal to one provided
        public async Task<User> GetUser(string userId)
        {
            return await db.Users
                            .Where(u => u.Id == userId)
                            .SingleAsync();
        }

        //This method returns role that has name equal to one provided
        public async Task<Role> GetRole(string roleName)
        {
            return await db.Roles
                            .Where(r => r.Name == roleName)
                            .SingleAsync();
        }

        public string GetRoleId(string roleName)
        {
            return db.Roles
                        .Where(r => r.Name == roleName)
                        .Select(r => r.Id)
                        .Single();
        }

        public async Task<string> GetRoleIdByUserId(string userId)
        {
            return await db.UserRoles
                                .Where(ur => ur.UserId == userId)
                                .Select(ur => ur.RoleId)
                                .SingleAsync();
        }

        public async Task<string> GetRoleName(string userId)
        {
            var roleId = await GetRoleIdByUserId(userId);

            return await db.Roles
                            .Where(r => r.Id == roleId)
                            .Select(r => r.Name)
                            .SingleAsync();
        }

        //This method returns all roles
        public async Task<List<Role>> GetRoles()
        {
            return await db.Roles.ToListAsync();
        }

        //This method checks if there is any Subscription with UserId as provided
        public async Task<bool> IsUserSubscribed(string userId)
        {
            int subscriptionsNumber = await db.Subscriptions
                                                .Where(s => s.UserId == userId)
                                                .Where(s => s.IsActive == true)
                                            .CountAsync();
            return subscriptionsNumber > 0;
        }

        public async Task CreateSubscription(int planId, string userId)
        {
            var subscription = new Subscription
            {
                IsActive = true,
                PlanId = planId,
                UserId = userId
            };
            await db.Subscriptions.AddAsync(subscription);
            await db.SaveChangesAsync();
        }
        
        //This method creates a new Subscription
        public async Task StartSubscription(int planId, string userId)
        {
            if (await IsUserSubscribed(userId)) //check if user has any subscription
                await EndSubscription(userId);

            await CreateSubscription(planId, userId);
        }

        //This method creates a new Invoice for created Subscription
        public async Task CreateInvoice(string userId)
        {
            var subscription = await GetSubscription(userId);
            var invoice = new Invoice()
            {
                Description = "User with ID: " + userId + " has activated Subscription on: " + subscription.StartTimeStamp + ". Plan name: " + subscription.Plan.PlanName + ". Expires on: " + subscription.EndTimeStamp + ".",
                Amount = subscription.Plan.Price,
                SubscriptionId = subscription.SubscriptionId
            };
            await db.Invoices.AddAsync(invoice);
            await db.SaveChangesAsync();
        }

        //This method sets Subscription IsActive to false for provided user 
        public async Task EndSubscription(string userId)
        {
            var subscription = await db.Subscriptions
                                        .Where(s => s.UserId == userId)
                                        .Where(s => s.IsActive == true)
                                        .SingleAsync();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.RemoveFromRoleAsync(user, await GetRoleName(userId));
            await userManager.AddToRoleAsync(user, "Basic");
            subscription.IsActive = false;
            await db.SaveChangesAsync();
        }

        //This method returns SubscriptionId for Subscription that is active for provided User
        public async Task<int> GetSubscriptionId(string userId)
        {
            return await db.Subscriptions
                            .Where(s => s.UserId == userId)
                            .Where(s => s.IsActive == true)
                            .Select(s => s.SubscriptionId)
                            .SingleAsync();
        }

        //This method returns active Subscription for provided User
        public async Task<Subscription> GetSubscription(string userId)
        {
            return await db.Subscriptions
                            .Where(s => s.UserId == userId)
                            .Where(s => s.IsActive == true)
                            .Include(s => s.Plan)
                            .SingleAsync();
        }

        //This method returns Expiration Date of Subscription for provided User
        public async Task<DateTime> GetSubscriptionExpDate(string userId)//nije reseno ako korisnik nema aktivnu pretplatu
        {
            return await db.Subscriptions
                            .Where(s => s.IsActive == true)
                            .Where(s => s.UserId == userId)
                            .Select(s => s.EndTimeStamp)
                            .SingleAsync();
        }

        //This method sets User Role to Basic if Subscription has expired
        public async Task CheckSubscriptionStatus(string userId, int subscriptionId)
        {
            var subscription = await GetSubscription(userId);
            if (subscription.EndTimeStamp < DateTime.Now)
            {
                await StartSubscription(1, userId);
                subscription.IsActive = false;
                db.SaveChanges();
            }
        }

        //This method returns Plan by Id provided
        public async Task<Plan> GetPlan(int planId)
        {
            return await db.Plans
                            .Where(p => p.PlanId == planId)
                            .SingleAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByUserId(string userId)
        {
            return await db.Invoices
                            .Where(i => i.Subscription.UserId == userId)
                            .Include(i => i.Subscription)
                            .OrderByDescending(i => i.CreatedTimeStamp)
                            .ToListAsync();
        }

        public async Task<List<Message>> GetReceivedMessagesByUserId(string userId)
        {
            return await db.Messages
                            .Where(m => m.ReceiverId == userId)
                            .OrderByDescending(m => m.TimeSent)
                            .ToListAsync();
        }

        public async Task<List<Message>> GetSentMessagesByUserId(string userId)
        {
            return await db.Messages
                            .Where(m => m.SenderId == userId)
                            .OrderByDescending(m => m.TimeSent)
                            .ToListAsync();
        }
    }
}
