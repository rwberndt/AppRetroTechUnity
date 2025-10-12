using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// Abstraction used by the game to request domain data from the backing API.
    /// </summary>
    public interface IContentService
    {
        Task<IReadOnlyList<Category>> GetCategoriesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        Task<Category> GetCategoryByIdAsync(long id, bool forceRefresh = false, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ComputerPiece>> GetPiecesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        Task<ComputerPiece> GetPieceByIdAsync(long id, bool forceRefresh = false, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuizQuestion>> GetQuizQuestionsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    }
}
