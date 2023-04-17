using DataAccessLibrary.DataAccess;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLibrary.Repository.SqlRepository
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly ILeagueRepository leagueRepository;
        private readonly ITipRepository tipRepository;
        private readonly XbetContext db;

        public StatisticsRepository(ILeagueRepository leagueRepository, ITipRepository tipRepository, XbetContext db)
        {
            this.leagueRepository = leagueRepository;
            this.tipRepository = tipRepository;
            this.db = db;
        }
        public async Task<List<Prediction>> GetPredictionsPrevious(int tipTypeId)
        {
            return await db.Predictions
                                .Where(p => p.IsCorrect != null && p.Tip.TipType.TipTypeId == tipTypeId)
                                .Include(p => p.Match)
                                    .ThenInclude(p => p.League)
                                .Include(p => p.Tip)
                                .OrderByDescending(p => p.Match.MatchDateTime)
                                    .ThenBy(p => p.Match.League.LeagueId)
                            .ToListAsync();
        }

        public async Task<ConcurrentDictionary<League, int[]>> GetLeagueStats(int tipTypeId)
        {
            var leagueStats = new ConcurrentDictionary<League, int[]>();

            foreach (var item in await db.Leagues.ToListAsync())
            {
                int Total = await leagueRepository.GetLeagueTotalPlayed(item.LeagueId, tipTypeId);
                int Wins = await leagueRepository.GetLeagueWins(item.LeagueId, tipTypeId);

                if (Total != 0)
                    leagueStats.TryAdd(item, new int[] { Total, Wins });

            }
            return leagueStats;
        }

        public async Task<ConcurrentDictionary<Tip, decimal[]>> GetTipStats(int tipTypeId)
        {
            var tipStats = new ConcurrentDictionary<Tip, decimal[]>();


            foreach (var item in await GetTipsByTipType(tipTypeId))
            {
                decimal Odds = await tipRepository.GetTipAverageOdds(item.TipId);
                decimal Total = await tipRepository.GetTipTotalPlayed(item.TipId);
                decimal Wins = await tipRepository.GetTipWins(item.TipId);

                if (Total != 0)
                    tipStats.TryAdd(item, new decimal[] { Odds, Total, Wins });
            }
            return tipStats;
        }

        public async Task<ConcurrentDictionary<Tip, decimal[]>> GetTipStatsByLeague(int tipTypeId, int leagueId)
        {
            var tipStats = new ConcurrentDictionary<Tip, decimal[]>();

            foreach (var item in await GetTipsByLeagueAndTipType(leagueId, tipTypeId))
            {
                decimal Odds = await tipRepository.GetTipAverageOddsByLeague(item.TipId, leagueId);
                decimal Total = await tipRepository.GetTipTotalPlayedByLeague(item.TipId, leagueId);
                decimal Wins = await tipRepository.GetTipWinsByLeague(item.TipId, leagueId);

                if (Total != 0)
                    tipStats.TryAdd(item, new decimal[] { Odds, Total, Wins });
            }

            return tipStats;

        }


        public async Task<List<Tip>> GetTipsByTipType(int tipTypeId)
        {
            return await db.Predictions
                                    .Where(p => p.Tip.TipType.TipTypeId == tipTypeId)
                                    .Where(p => p.IsCorrect != null)
                                    .Include(p => p.Tip)
                                        .ThenInclude(p => p.TipType)
                                .Select(p => p.Tip)
                                .ToListAsync();
        }

        public async Task<List<Tip>> GetTipsByLeagueAndTipType(int leagueId, int tipTypeId)
        {
            return await db.Predictions
                                            .Include(p => p.Match)
                                            .Include(p => p.Tip)
                                        .Where(p => p.Match.LeagueId == leagueId)
                                        .Where(p => p.Tip.TipType.TipTypeId == tipTypeId)
                                        .Where(p => p.IsCorrect != null)
                                    .OrderByDescending(p => p.Match.MatchDateTime)
                                    .Select(p => p.Tip)
                                    .ToListAsync();
        }

        public async Task<decimal[]> GetTipTypeStats(int tipTypeId)
        {
            decimal AverageOdds = await GetTipTypeAverageOdds(tipTypeId);
            decimal TotalPlayed = await GetTipTypeTotalPlayed(tipTypeId);
            decimal Wins = await GetTipTypeWins(tipTypeId);

            return new decimal[] { AverageOdds, TotalPlayed, Wins };
        }

        public async Task<decimal> GetTipTypeAverageOdds(int tipTypeId)
        {
            return await db.Predictions
                                    .Include(p => p.Tip)
                                        .ThenInclude(p => p.TipType)
                                    .Where(p => p.Tip.TipType.TipTypeId == tipTypeId)
                                    .Where(p => p.IsCorrect == true)
                                .AverageAsync(p => p.Odds);
        }

        public async Task<decimal> GetTipTypeTotalPlayed(int tipTypeId)
        {
            return await db.Predictions
                                        .Include(p => p.Tip)
                                            .ThenInclude(p => p.TipType)
                                        .Where(p => p.Tip.TipType.TipTypeId == tipTypeId)
                                        .Where(p => p.IsCorrect != null)
                                    .CountAsync();
        }

        public async Task<decimal> GetTipTypeWins(int tipTypeId)
        {
            return await db.Predictions
                                .Include(p => p.Tip)
                                    .ThenInclude(p => p.TipType)
                                .Where(p => p.Tip.TipType.TipTypeId == tipTypeId)
                                .Where(p => p.IsCorrect == true)
                            .CountAsync();
        }


        public async Task<ConcurrentDictionary<Tip, decimal[]>> GetTipStatsByTipAndLeague(int tipId, int leagueId)
        {
            var tipStats = new ConcurrentDictionary<Tip, decimal[]>();

            var tip = await tipRepository.GetTipByTipId(tipId);
            decimal odds = await tipRepository.GetTipAverageOddsByLeague(tipId, leagueId);
            decimal totalPlayed = await tipRepository.GetTipTotalPlayedByLeague(tipId, leagueId);
            decimal wins = await tipRepository.GetTipWinsByLeague(tipId, leagueId);
            tipStats.TryAdd(tip, new decimal[] { odds, totalPlayed, wins });
            return tipStats;
        }

        public async Task<List<Prediction>> GetPredictionsByTipId(int tipId)
        {
            return await db.Predictions
                                .Include(p => p.Match)
                                    .ThenInclude(p => p.League)
                                .Include(p => p.Tip)
                                .Where(p => p.IsCorrect != null)
                                .Where(p => p.TipId == tipId)
                                .OrderByDescending(p => p.Match.MatchDateTime)
                                    .ThenBy(p => p.Match.League.LeagueId)
                                .ToListAsync();
        }

        //This method returns dictionary including tip and its stats
        public async Task<ConcurrentDictionary<Tip, decimal[]>> GetTipStatsByTipId(int tipId)
        {
            var tipStats = new ConcurrentDictionary<Tip, decimal[]>();

            var tip = await tipRepository.GetTipByTipId(tipId);

            decimal odds = await tipRepository.GetTipAverageOdds(tipId);
            decimal total = await tipRepository.GetTipTotalPlayed(tipId);
            decimal wins = await tipRepository.GetTipWins(tipId);

            tipStats.TryAdd(tip, new decimal[] { odds, total, wins });

            return tipStats;
        }

        public async Task<Dictionary<League, int[]>> GetLeagueStatsByTip(int tipId)
        {
            var leagueStats = new Dictionary<League, int[]>();

            foreach (var item in await leagueRepository.GetLeagues())
            {
                int total = await leagueRepository.GetLeagueTotalPlayedByTip(item.LeagueId, tipId);
                int wins = await leagueRepository.GetLeagueWinsByTip(item.LeagueId, tipId);

                if (total != 0)
                    leagueStats.TryAdd(item, new int[] { total, wins });
            }

            return leagueStats;
        }
    }
}
