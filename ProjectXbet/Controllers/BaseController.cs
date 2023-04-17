using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using DataAccessLibrary.Repository.SqlRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    public abstract class BaseController : Controller
    {
        public string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier).Value;

        public string GetUserRoleName() => User.FindFirst(ClaimTypes.Role).Value;
    }
}
