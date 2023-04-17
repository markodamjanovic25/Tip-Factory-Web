using DataAccessLibrary.DataAccess;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Repository.SqlRepository
{
    public class TipRepository : ITipRepository
    {
        private readonly XbetContext db;

        public TipRepository(XbetContext db)
        {
            this.db = db;
        }
        
        public async Task<List<Tip>> GetTips()
        {
            return await db.Tips.ToListAsync();
        }
        public async Task<Tip> GetTipByTipId(int tipId)
        {
            return await db.Tips
                                .Where(t => t.TipId == tipId)
                            .SingleAsync();
        }
        public async Task<decimal> GetTipAverageOdds(int tipId)
        {
            return await db.Predictions
                            .Where(p => p.TipId == tipId)
                        .Select(p => p.Odds)
                        .AverageAsync();
        }

        public async Task<decimal> GetTipTotalPlayed(int tipId)
        {
            return await db.Predictions
                            .Where(p => p.TipId == tipId)
                            .Where(p => p.IsCorrect != null)
                        .CountAsync();
        }

        public async Task<decimal> GetTipWins(int tipId)
        {
            return await db.Predictions
                            .Where(p => p.TipId == tipId)
                            .Where(p => p.IsCorrect == true)
                        .CountAsync();
        }

        public async Task<decimal> GetTipAverageOddsByLeague(int tipId, int leagueId)
        {
            return await db.Predictions
                                        .Where(p => p.TipId == tipId)
                                        .Include(p => p.Match)
                                        .Where(p => p.Match.LeagueId == leagueId)
                                    .Select(p => p.Odds)
                                    .AverageAsync();
        }

        public async Task<decimal> GetTipTotalPlayedByLeague(int tipId, int leagueId)
        {
            return await db.Predictions
                                        .Where(p => p.TipId == tipId)
                                        .Include(p => p.Match)
                                        .Where(p => p.Match.LeagueId == leagueId)
                                        .Where(p => p.IsCorrect != null)
                                    .CountAsync();
        }

        public async Task<decimal> GetTipWinsByLeague(int tipId, int leagueId)
        {
            return await db.Predictions
                                        .Where(p => p.TipId == tipId)
                                        .Include(p => p.Match)
                                        .Where(p => p.Match.LeagueId == leagueId)
                                        .Where(p => p.IsCorrect == true)
                                    .CountAsync();
        }

    }
}
