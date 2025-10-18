using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RetroTech;

namespace RetroTech.Services
{
    /// <summary>
    /// High level service that exposes the domain specific endpoints of the RetroTech API.
    /// The service caches responses to avoid unnecessary network traffic.
    /// </summary>
    public class ContentService : IContentService
    {
        private readonly IApiClient _apiClient;
        private readonly ApiConfiguration _configuration;
        private readonly SemaphoreSlim _categoriesLock = new(1, 1);
        private readonly SemaphoreSlim _piecesLock = new(1, 1);
        private readonly SemaphoreSlim _quizLock = new(1, 1);

        private List<Category> _categoriesCache;
        private List<ComputerPiece> _piecesCache;
        private List<QuizQuestion> _quizCache;

        public ContentService(IApiClient apiClient, ApiConfiguration configuration)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IReadOnlyList<Category>> GetCategoriesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && _categoriesCache != null && _categoriesCache.Count > 0)
            {
                return _categoriesCache;
            }

            await _categoriesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (forceRefresh)
                {
                    _categoriesCache = null;
                }

                if (_categoriesCache != null && _categoriesCache.Count > 0)
                {
                    return _categoriesCache;
                }

                var response = await _apiClient
                    .GetCollectionAsync<CategoryDto>(_configuration.CategoriesEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _categoriesCache = MapCategories(response);
                return _categoriesCache;
            }
            finally
            {
                _categoriesLock.Release();
            }
        }

        public async Task<IReadOnlyList<ComputerPiece>> GetPiecesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && _piecesCache != null && _piecesCache.Count > 0)
            {
                return _piecesCache;
            }

            await _piecesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (forceRefresh)
                {
                    _piecesCache = null;
                }

                if (_piecesCache != null && _piecesCache.Count > 0)
                {
                    return _piecesCache;
                }

                var response = await _apiClient
                    .GetCollectionAsync<ComputerPieceDto>(_configuration.PiecesEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _piecesCache = MapPieces(response);
                return _piecesCache;
            }
            finally
            {
                _piecesLock.Release();
            }
        }

        public async Task<Category> GetCategoryByIdAsync(long id, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            await _categoriesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!forceRefresh && _categoriesCache != null)
                {
                    var cached = _categoriesCache.Find(category => category.Id == id);
                    if (cached != null)
                    {
                        return cached;
                    }
                }

                if (forceRefresh && _categoriesCache != null)
                {
                    _categoriesCache.RemoveAll(category => category.Id == id);
                }

                var dto = await _apiClient
                    .GetAsync<CategoryDto>($"{_configuration.CategoriesEndpoint}/{id}", cancellationToken)
                    .ConfigureAwait(false);

                var category = MapCategory(dto);
                if (category != null)
                {
                    _categoriesCache ??= new List<Category>();
                    int index = _categoriesCache.FindIndex(existing => existing.Id == category.Id);
                    if (index >= 0)
                    {
                        _categoriesCache[index] = category;
                    }
                    else
                    {
                        _categoriesCache.Add(category);
                    }
                }

                return category;
            }
            finally
            {
                _categoriesLock.Release();
            }
        }

        public async Task<ComputerPiece> GetPieceByIdAsync(long id, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            await _piecesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!forceRefresh && _piecesCache != null)
                {
                    var cached = _piecesCache.Find(piece => piece.Id == id);
                    if (cached != null)
                    {
                        return cached;
                    }
                }

                if (forceRefresh && _piecesCache != null)
                {
                    _piecesCache.RemoveAll(piece => piece.Id == id);
                }

                var dto = await _apiClient
                    .GetAsync<ComputerPieceDto>($"{_configuration.PiecesEndpoint}/{id}", cancellationToken)
                    .ConfigureAwait(false);

                var piece = MapPiece(dto);
                if (piece != null)
                {
                    _piecesCache ??= new List<ComputerPiece>();
                    int index = _piecesCache.FindIndex(existing => existing.Id == piece.Id);
                    if (index >= 0)
                    {
                        _piecesCache[index] = piece;
                    }
                    else
                    {
                        _piecesCache.Add(piece);
                    }
                }

                return piece;
            }
            finally
            {
                _piecesLock.Release();
            }
        }

        public async Task<IReadOnlyList<QuizQuestion>> GetQuizQuestionsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && _quizCache != null && _quizCache.Count > 0)
            {
                return _quizCache;
            }

            await _quizLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (forceRefresh)
                {
                    _quizCache = null;
                }

                if (_quizCache != null && _quizCache.Count > 0)
                {
                    return _quizCache;
                }

                var response = await _apiClient
                    .GetCollectionAsync<QuizQuestionDto>(_configuration.QuizEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _quizCache = MapQuizQuestions(response);
                return _quizCache;
            }
            finally
            {
                _quizLock.Release();
            }
        }

        private static List<Category> MapCategories(IReadOnlyList<CategoryDto> source)
        {
            var result = new List<Category>();
            if (source == null)
            {
                return result;
            }

            foreach (var dto in source)
            {
                var mapped = MapCategory(dto);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        private static Category MapCategory(CategoryDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new Category(
                dto.id,
                dto.name ?? string.Empty,
                dto.subcategories != null ? new List<string>(dto.subcategories) : new List<string>());
        }

        private static List<ComputerPiece> MapPieces(IReadOnlyList<ComputerPieceDto> source)
        {
            var result = new List<ComputerPiece>();
            if (source == null)
            {
                return result;
            }

            foreach (var dto in source)
            {
                var mapped = MapPiece(dto);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        private static ComputerPiece MapPiece(ComputerPieceDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new ComputerPiece(
                dto.id,
                dto.name ?? string.Empty,
                dto.categoryId,
                dto.yearManufactured,
                dto.manufacturer,
                dto.description,
                dto.imageUrl ?? dto.imageData ?? string.Empty,
                dto.curiosities,
                dto.specifications != null ? new List<string>(dto.specifications) : new List<string>(),
                dto.subcategory);
        }

        private static List<QuizQuestion> MapQuizQuestions(IReadOnlyList<QuizQuestionDto> source)
        {
            var result = new List<QuizQuestion>();
            if (source == null)
            {
                return result;
            }

            foreach (var dto in source)
            {
                var mapped = MapQuizQuestion(dto);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        private static QuizQuestion MapQuizQuestion(QuizQuestionDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new QuizQuestion(
                dto.id,
                dto.question ?? string.Empty,
                dto.options != null ? new List<string>(dto.options) : new List<string>(),
                dto.correctAnswerIndex,
                dto.explanation,
                dto.relatedPieceId > 0 ? dto.relatedPieceId : (long?)null);
        }

        [Serializable]
        private class CategoryDto
        {
            public long id;
            public string name;
            public string[] subcategories;
        }

        [Serializable]
        private class ComputerPieceDto
        {
            public long id;
            public string name;
            public long categoryId;
            public int yearManufactured;
            public string manufacturer;
            public string description;
            public string imageUrl;
            public string imageData;
            public string imageContentType;
            public string curiosities;
            public string[] specifications;
            public string subcategory;
        }

        [Serializable]
        private class QuizQuestionDto
        {
            public long id;
            public string question;
            public string[] options;
            public int correctAnswerIndex;
            public string explanation;
            public long relatedPieceId;
        }
    }
}
