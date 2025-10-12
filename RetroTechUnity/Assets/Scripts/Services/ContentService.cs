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
                    .GetCollectionAsync<Category>(_configuration.CategoriesEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _categoriesCache = Materialise(response);
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
                    .GetCollectionAsync<ComputerPiece>(_configuration.PiecesEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _piecesCache = Materialise(response);
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

                var category = await _apiClient
                    .GetAsync<Category>($"{_configuration.CategoriesEndpoint}/{id}", cancellationToken)
                    .ConfigureAwait(false);

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

                var piece = await _apiClient
                    .GetAsync<ComputerPiece>($"{_configuration.PiecesEndpoint}/{id}", cancellationToken)
                    .ConfigureAwait(false);

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
                    .GetCollectionAsync<QuizQuestion>(_configuration.QuizEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _quizCache = Materialise(response);
                return _quizCache;
            }
            finally
            {
                _quizLock.Release();
            }
        }

        private static List<T> Materialise<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return new List<T>();
            }

            return source as List<T> ?? new List<T>(source);
        }
    }
}
