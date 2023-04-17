using DataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Repository.IRepository
{
    public interface IPredictionRepository
    {
        Task AddPrediction(Prediction prediction, string role);
        Task<Prediction> GetPredictionByIdAsync(int predictionId);
        Task<List<Prediction>> GetPredictionsAll();
        Task<List<Prediction>> GetPredictionsByTipType(string roleName, int tipTypeId);
        Task<decimal> GetOddsByPredictionId(int predictionId);
        Task SetPredictionOutcomeAsync(int predictionId, int clubHomeGoalsHalf, int clubAwayGoalsHalf, int clubHomeGoals, int clubAwayGoals, bool isCorrect);
        Task DeletePrediction(int predictionId);
        Task EditPredictionAsync(Prediction prediction);
    }
}
