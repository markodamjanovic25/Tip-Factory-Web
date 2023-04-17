﻿using DataAccessLibrary.DataAccess;
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
    public class LeagueRepository : ILeagueRepository
    {
        private readonly XbetContext db;
        public LeagueRepository(XbetContext db)
        {
            this.db = db;
        }

        public async Task<List<League>> GetLeagues()
        {
            return await db.Leagues.ToListAsync();
        }

        public async Task<int> GetLeagueTotalPlayed(int LeagueId, int TipTypeId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .Where(p => p.IsCorrect != null)
                        .CountAsync();
        }

        public async Task<int> GetLeagueWins(int LeagueId, int TipTypeId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .Where(p => p.IsCorrect == true)
                        .CountAsync();
        }

        public async Task<decimal> GetLeagueAverageOdds(int LeagueId, int TipTypeId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .Where(p => p.IsCorrect == true)
                                .Select(p => p.Odds)
                                .AverageAsync();
        }

        public async Task<decimal> GetLeagueAverageOddsByTip(int LeagueId, int TipId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.TipId == TipId)
                                .Where(p => p.IsCorrect == true)
                                .Select(p => p.Odds)
                                .AverageAsync();
        }

        public async Task<int> GetLeagueTotalPlayedByTip(int LeagueId, int TipId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.TipId == TipId)
                                .Where(p => p.IsCorrect != null)
                        .CountAsync();
        }

        public async Task<int> GetLeagueWinsByTip(int LeagueId, int TipId)
        {
            return await db.Predictions
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.Tip.TipId == TipId)
                                .Where(p => p.IsCorrect == true)
                        .CountAsync();
        }

        public async Task<League> GetLeagueByIdAsync(int LeagueId)
        {
            return await db.Leagues
                                .Where(l => l.LeagueId == LeagueId)
                        .SingleAsync();
        }

        public async Task<List<decimal>> GetAllOddsByTipType(int TipTypeId)
        {
            return await db.Predictions
                                .Include(p => p.Tip)
                                    .ThenInclude(p => p.TipType)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .OrderBy(p => p.Odds)
                                .Select(p => p.Odds)
                                .Distinct()
                                .ToListAsync();
        }

        public async Task<Dictionary<League, List<int[]>>> GetLeagueStatsByOdds(int TipTypeId)
        {
            Dictionary<League, List<int[]>> LeagueWithStats = new Dictionary<League, List<int[]>>();
            
                foreach(var LeagueItem in await GetLeagues())
                {
                    List<int[]> Stats = new List<int[]>();
                    foreach (var OddsItem in await GetAllOddsByTipType(TipTypeId))
                        {
                            int Total = await GetPredictionTotalByOddsAndLeague(TipTypeId, OddsItem, LeagueItem.LeagueId);
                            int Wins = await GetPredictionWinsByOddsAndLeague(TipTypeId, OddsItem, LeagueItem.LeagueId);
                            Stats.Add(new int[] { Total, Wins});
                        }
                    LeagueWithStats.Add(LeagueItem, Stats);
                }
            
            return LeagueWithStats;
        }

        public async Task<int> GetPredictionTotalByOddsAndLeague(int TipTypeId, decimal Odds, int LeagueId)
        {
            return await db.Predictions
                                .Include(p => p.Match)
                                    .ThenInclude(p => p.League)
                                .Include(p => p.Tip)
                                    .ThenInclude(p => p.TipType)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .Where(p => p.Odds == Odds)
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .CountAsync();
        }

        public async Task<int> GetPredictionWinsByOddsAndLeague(int TipTypeId, decimal Odds, int LeagueId)
        {
            return await db.Predictions
                                .Include(p => p.Match)
                                    .ThenInclude(p => p.League)
                                .Include(p => p.Tip)
                                    .ThenInclude(p => p.TipType)
                                .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                .Where(p => p.Odds == Odds)
                                .Where(p => p.Match.LeagueId == LeagueId)
                                .Where(p => p.IsCorrect == true)
                                .CountAsync();
        }

        public async Task<List<Prediction>> GetPredictionsByLeagueAndTipType(int LeagueId, int TipTypeId)
        {
            return await db.Predictions
                                            .Include(p => p.Match)
                                            .Include(p => p.Tip)
                                        .Where(p => p.IsCorrect != null)
                                        .Where(p => p.Match.LeagueId == LeagueId)
                                        .Where(p => p.Tip.TipType.TipTypeId == TipTypeId)
                                    .OrderByDescending(p => p.Match.MatchDateTime)
                                    .ToListAsync();
        }

        public async Task<List<Prediction>> GetPredictionsByLeagueAndTip(int LeagueId, int TipId)
        {
            return await db.Predictions
                                        .Include(p => p.Match)
                                        .Where(p => p.IsCorrect != null)
                                        .Where(p => p.Match.LeagueId == LeagueId)
                                        .Where(p => p.TipId == TipId)
                                    .OrderByDescending(p => p.Match.MatchDateTime)
                                    .ToListAsync();
        }

    }
}
