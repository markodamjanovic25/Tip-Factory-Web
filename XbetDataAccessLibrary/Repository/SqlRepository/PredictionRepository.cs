using DataAccessLibrary.DataAccess;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Repository.SqlRepository
{
    public class PredictionRepository : IPredictionRepository
    {
        private readonly IAccountRepository accountRepository;
        private readonly XbetContext db;

        public PredictionRepository(IAccountRepository accountRepository, XbetContext db)
        {
            this.accountRepository = accountRepository;
            this.db = db;
        }

        public async Task AddPrediction(Prediction prediction, string role)
        {
            await db.Predictions.AddAsync(prediction);
            await db.UserRolePredictions.AddAsync(new UserRolePredictions() { UserRoleId = role, PredictionId = prediction.PredictionId });
            await db.SaveChangesAsync();
        }

        public async Task<Prediction> GetPredictionByIdAsync(int predictionId)
        {
            return await db.Predictions
                .Where(p => p.PredictionId == predictionId)
                    .Include(p => p.Tip)
                        .ThenInclude(p => p.TipType)
                        .SingleAsync();
        }

        //This method returns list of all predictions that have IsCorrect set to null
        public async Task<List<Prediction>> GetPredictionsAll()
        {
            return await db.Predictions
                .Where(p => p.IsCorrect == null)
                .Include(p => p.Tip)
                .Include(p => p.Match)
                    .ThenInclude(p => p.League)
                .OrderByDescending(p => p.Match.MatchDateTime)
                    .ThenBy(p => p.Match.League.LeagueId)
                .ToListAsync();
        }

        // This method returns list of predictions this user role has which are same type as provided
        public async Task<List<Prediction>> GetPredictionsByTipType(string userRoleName, int tipTypeId) 
        {
            var userRolePredictions = await db.UserRolePredictions
                                                .Where(p => p.UserRoleId == accountRepository.GetRoleId(userRoleName))
                                                .Where(p => p.Prediction.Tip.TipType.TipTypeId == tipTypeId)
                                                .Where(p => p.Prediction.IsCorrect == null)
                                                .Include(p => p.Prediction)
                                                    .ThenInclude(p => p.Tip)
                                                .Include(p => p.Prediction)
                                                    .ThenInclude(p => p.Match)
                                                        .ThenInclude(p => p.League)
                                                .OrderBy(p => p.Prediction.Match.MatchDateTime)
                                                    .ThenBy(p => p.Prediction.Match.League.LeagueId)
                                            .ToListAsync();

            var list = new List<Prediction>();

            foreach (var item in userRolePredictions)
            {
                list.Add((await db.Predictions
                    .Where(p => p.PredictionId == item.PredictionId)
                    .SingleAsync()));
            }

            return list;
        }

        //This method returns Odds of Prediction with PredictionId as provided
        public async Task<decimal> GetOddsByPredictionId(int predictionId)
        {
            return await db.Predictions
                            .Where(p => p.PredictionId == predictionId)
                        .Select(p => p.Odds)
                        .SingleAsync();
        }

        public async Task SetPredictionOutcomeAsync(int predictionId, int clubHomeGoalsHalf, int clubAwayGoalsHalf, int clubHomeGoals, int clubAwayGoals, bool isCorrect)
        {
            Prediction p = await db.Predictions
                                    .Where(p => p.PredictionId == predictionId)
                                    .Include(p => p.Match)
                                .SingleAsync();

            p.Match.ClubHomeGoalsHalf = clubHomeGoalsHalf;
            p.Match.ClubAwayGoalsHalf = clubAwayGoalsHalf;
            p.Match.ClubHomeGoals = clubHomeGoals;
            p.Match.ClubAwayGoals = clubAwayGoals;
            p.IsCorrect = isCorrect;
            await db.SaveChangesAsync();
        }
        
        public async Task DeletePrediction(int predictionId)
        {
            Prediction toDelete = await GetPredictionByIdAsync(predictionId);
            db.Predictions.Remove(toDelete);
            await db.SaveChangesAsync();
        }

        public async Task EditPredictionAsync(Prediction prediction)
        {
            Prediction toEdit = await GetPredictionByIdAsync(prediction.PredictionId);
            toEdit.Odds = prediction.Odds;
            toEdit.Chance = prediction.Chance;
            toEdit.TipId = prediction.TipId;
            await db.SaveChangesAsync();
        }
    }
}
