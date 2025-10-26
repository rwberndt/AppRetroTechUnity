using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RetroTech.Services;
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página da linha do tempo do RetroTech que exibe as peças computacionais
    /// organizadas cronologicamente por ano de fabricação.
    /// Permite navegação através da história da computação e acesso aos detalhes das peças.
    /// </summary>
    public class TimelinePage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "TimelinePage";

        [Header("Visual Configuration")]
        [SerializeField] private float titleMarginBottom = 24f;
        [SerializeField] private float timelineCardHeight = 200f;
        [SerializeField] private int pageTitleFontSize = 36;
        [SerializeField] private int yearFontSize = 30;
        [SerializeField] private int nameCardFontSize = 22;
        [SerializeField] private int descriptionFontSize = 24;
        [SerializeField] private int buttonFontSize = 28;
        [SerializeField] private float buttonHeight = 40f;
        [SerializeField] private float thumbnailHeight = 200f;

        // Colors
        private readonly Color PageTitleColor = Color.white;
        private readonly Color TimelineCardColor = new Color(1f, 1f, 1f, 0.1f); // Match glass card tone
        private readonly Color YearBadgeColor = new Color32(147, 112, 219, 255); // Purple
        private readonly Color PieceNameColor = Color.white;
        private readonly Color DescriptionColor = new Color32(255, 255, 255, 180);
        private readonly Color ThumbnailBackgroundColor = new Color(1f, 1f, 1f, 0.08f);

        // Sorting options
        public enum SortOrder
        {
            Ascending,  // Oldest first
            Descending  // Newest first
        }

        // Events
        public System.Action<ComputerPiece> OnPieceSelected;
        public System.Action<List<ComputerPiece>> OnTimelineGenerated; // When timeline is created/updated
        public System.Action<SortOrder> OnSortOrderChanged;

        // State
        private SortOrder _currentSortOrder = SortOrder.Ascending;
        private List<ComputerPiece> _sortedPieces;
        private List<ComputerPiece> _filteredPieces;
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private GameObject _timelineContainer;
        private System.Func<Transform, float, Image> _createGlassCardFunc;
        private System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> _createCTAButtonFunc;
        private ScrollRect _scrollRect;

        /// <summary>
        /// Cria e configura a página da linha do tempo
        /// </summary>
        /// <param name="parent">Transform pai onde a página será criada</param>
        /// <param name="buildSurfaceFunc">Função para criar a superfície base da página</param>
        /// <param name="createGlassCardFunc">Função para criar cards com efeito glass</param>
        /// <param name="createCTAButtonFunc">Função para criar botões CTA</param>
        /// <returns>GameObject da página criada</returns>
        public GameObject CreatePage(
            Transform parent,
            System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc,
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            ConfigureSurfaceForTimeline(surface, content);

            CreateTimelineContent(createGlassCardFunc, createCTAButtonFunc);

            RefreshScrollMetrics();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo da linha do tempo
        /// </summary>
        private void CreateTimelineContent(
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            // Page title
            CreatePageTitle();

            // Sort controls (optional - can be enabled later)
            // CreateSortControls(createCTAButtonFunc);

            // Timeline container
            CreateTimelineContainer();

            // Generate timeline
            GenerateTimeline(createGlassCardFunc, createCTAButtonFunc);
        }

        /// <summary>
        /// Cria o título da página
        /// </summary>
        private void CreatePageTitle()
        {
            var titleContainer = new GameObject("PageTitle", typeof(RectTransform), typeof(LayoutElement));
            titleContainer.transform.SetParent(_contentContainer, false);

            var titleRT = titleContainer.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var titleLayout = titleContainer.GetComponent<LayoutElement>();
            float titleHeight = ResponsiveTypography.ResponsiveSpacing(pageTitleFontSize + 32f);
            titleLayout.minHeight = titleHeight;
            titleLayout.preferredHeight = titleHeight;
            titleLayout.flexibleHeight = 0f;

            var titleTMP = UiKit.TMP(titleContainer.transform, "Linha do Tempo", pageTitleFontSize,
                PageTitleColor, TextAlignmentOptions.Left, bold: true);
            var tmpRT = titleTMP.rectTransform;
            tmpRT.anchorMin = Vector2.zero;
            tmpRT.anchorMax = Vector2.one;
            tmpRT.offsetMin = Vector2.zero;
            tmpRT.offsetMax = Vector2.zero;

            AddSpacer(_contentContainer.transform, 0f, titleMarginBottom);
        }

        /// <summary>
        /// Cria os controles de ordenação (opcional)
        /// </summary>
        private void CreateSortControls(System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var sortContainer = new GameObject("SortControls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            sortContainer.transform.SetParent(_contentContainer, false);

            var sortHLG = sortContainer.GetComponent<HorizontalLayoutGroup>();
            sortHLG.spacing = 12;
            sortHLG.childControlWidth = false;
            sortHLG.childForceExpandWidth = false;
            sortHLG.childAlignment = TextAnchor.MiddleLeft;

            var sortLE = sortContainer.AddComponent<LayoutElement>();
            sortLE.preferredHeight = 48f;
            sortLE.minHeight = 48f;

            // Sort ascending button
            var ascBtn = createCTAButtonFunc(sortContainer.transform, "Mais Antigos", () => ChangeSortOrder(SortOrder.Ascending));
            var ascLE = ascBtn.GetComponent<LayoutElement>();
            if (ascLE == null) ascLE = ascBtn.AddComponent<LayoutElement>();
            ascLE.preferredWidth = 120f;

            // Sort descending button  
            var descBtn = createCTAButtonFunc(sortContainer.transform, "Mais Recentes", () => ChangeSortOrder(SortOrder.Descending));
            var descLE = descBtn.GetComponent<LayoutElement>();
            if (descLE == null) descLE = descBtn.AddComponent<LayoutElement>();
            descLE.preferredWidth = 120f;

            // Add spacer
            AddSpacer(sortContainer.transform, 0, 24f);
        }

        /// <summary>
        /// Cria o container da linha do tempo
        /// </summary>
        private void CreateTimelineContainer()
        {
            _timelineContainer = new GameObject("Timeline", typeof(RectTransform));
            _timelineContainer.transform.SetParent(_contentContainer, false);

            var layoutGroup = _timelineContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 16f;
            layoutGroup.padding = new RectOffset(0, 0, 0, 24);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var contentSizeFitter = _timelineContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var timelineLE = _timelineContainer.AddComponent<LayoutElement>();
            timelineLE.flexibleHeight = 0f;
        }
        

        /// <summary>
        /// Gera a linha do tempo com as peças
        /// </summary>
        private void GenerateTimeline(
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            _createGlassCardFunc = createGlassCardFunc;
            _createCTAButtonFunc = createCTAButtonFunc;
            RefreshTimeline();
        }

        /// <summary>
        /// Ordena as peças de acordo com a ordem atual
        /// </summary>
        private void SortPieces()
        {
            var sourcePieces = _filteredPieces ?? SampleData.Pieces ?? new List<ComputerPiece>();
            _sortedPieces = new List<ComputerPiece>(sourcePieces);

            switch (_currentSortOrder)
            {
                case SortOrder.Ascending:
                    _sortedPieces.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured));
                    break;
                case SortOrder.Descending:
                    _sortedPieces.Sort((a, b) => b.YearManufactured.CompareTo(a.YearManufactured));
                    break;
            }

            // Secondary sort by name for pieces with same year
            _sortedPieces = _sortedPieces
                .OrderBy(p => _currentSortOrder == SortOrder.Ascending ? p.YearManufactured : -p.YearManufactured)
                .ThenBy(p => p.Name)
                .ToList();
        }

        /// <summary>
        /// Cria um card individual da timeline
        /// </summary>
        private GameObject CreateTimelineCard(
            Transform parent,
            ComputerPiece piece,
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var timelineCard = createGlassCardFunc(parent, timelineCardHeight);
            timelineCard.color = TimelineCardColor;

            var cardLayoutElement = timelineCard.GetComponent<LayoutElement>();
            if (cardLayoutElement != null)
            {
                cardLayoutElement.minHeight = timelineCardHeight;
                cardLayoutElement.preferredHeight = -1f;
                cardLayoutElement.flexibleHeight = 0f;
            }

            var cardFitter = timelineCard.gameObject.GetComponent<ContentSizeFitter>();
            if (cardFitter == null)
                cardFitter = timelineCard.gameObject.AddComponent<ContentSizeFitter>();
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var cardVLG = timelineCard.gameObject.GetComponent<VerticalLayoutGroup>();
            if (cardVLG == null)
                cardVLG = timelineCard.gameObject.AddComponent<VerticalLayoutGroup>();
            cardVLG.padding = new RectOffset(20, 20, 16, 16);
            cardVLG.spacing = 12;
            cardVLG.childAlignment = TextAnchor.UpperLeft;
            cardVLG.childControlWidth = true;
            cardVLG.childForceExpandWidth = true;
            cardVLG.childControlHeight = true;
            cardVLG.childForceExpandHeight = false;

            // Piece image
            CreatePieceThumbnail(timelineCard.transform, piece);

            // Year badge
            CreateYearBadge(timelineCard.transform, piece);

            // Piece name
            CreatePieceName(timelineCard.transform, piece);

            // Description
            CreatePieceDescription(timelineCard.transform, piece);

            // Details button
            CreateDetailsButton(timelineCard.transform, piece, createCTAButtonFunc);

            timelineCard.gameObject.name = $"TimelineCard_{piece.Name}_{piece.YearManufactured}";

            return timelineCard.gameObject;
        }

        private void CreatePieceThumbnail(Transform parent, ComputerPiece piece)
        {
            var frameGO = new GameObject("PieceThumbnail", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            frameGO.transform.SetParent(parent, false);
            frameGO.transform.SetAsFirstSibling();

            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = new Vector2(0f, 1f);
            frameRT.anchorMax = new Vector2(1f, 1f);
            frameRT.pivot = new Vector2(0.5f, 1f);
            frameRT.offsetMin = Vector2.zero;
            frameRT.offsetMax = Vector2.zero;

            var layout = frameGO.AddComponent<LayoutElement>();
            layout.preferredHeight = thumbnailHeight;
            layout.minHeight = thumbnailHeight;
            layout.flexibleHeight = 0f;

            var background = frameGO.GetComponent<Image>();
            background.color = ThumbnailBackgroundColor;
            background.raycastTarget = false;
            background.type = Image.Type.Simple;

            var sprite = PieceImageFactory.GetSprite(piece);
            if (sprite == null)
            {
                return;
            }

            var imageGO = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imageGO.transform.SetParent(frameGO.transform, false);

            var imageRT = imageGO.GetComponent<RectTransform>();
            imageRT.anchorMin = Vector2.zero;
            imageRT.anchorMax = Vector2.one;
            imageRT.offsetMin = new Vector2(12f, 12f);
            imageRT.offsetMax = new Vector2(-12f, -12f);

            var image = imageGO.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var fitter = imageGO.GetComponent<AspectRatioFitter>() ?? imageGO.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (sprite.rect.height > 0f)
            {
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }

        /// <summary>
        /// Cria o badge do ano
        /// </summary>
        private void CreateYearBadge(Transform parent, ComputerPiece piece)
        {
            var yearTMP = UiKit.TMP(parent, piece.YearManufactured.ToString(), yearFontSize,
                YearBadgeColor, TextAlignmentOptions.Left, bold: true);
            yearTMP.margin = new Vector4(0, 0, 0, 4);
            yearTMP.name = "YearBadge";
        }

        /// <summary>
        /// Cria o nome da peça
        /// </summary>
        private void CreatePieceName(Transform parent, ComputerPiece piece)
        {
            var nameTMP = UiKit.TMP(parent, piece.Name, nameCardFontSize,
                PieceNameColor, TextAlignmentOptions.Left, bold: true);
            nameTMP.enableWordWrapping = true;
            nameTMP.margin = new Vector4(0, 0, 0, 8);
            nameTMP.name = "PieceName";
        }

        /// <summary>
        /// Cria a descrição da peça
        /// </summary>
        private void CreatePieceDescription(Transform parent, ComputerPiece piece)
        {
            string description = "";
            if (!string.IsNullOrEmpty(piece.Manufacturer))
                description += piece.Manufacturer;

            if (!string.IsNullOrEmpty(piece.Description))
            {
                if (!string.IsNullOrEmpty(description))
                    description += " — ";
                description += piece.Description;
            }

            if (string.IsNullOrEmpty(description))
                description = "Informações não disponíveis";

            var descTMP = UiKit.TMP(parent, description, descriptionFontSize,
                DescriptionColor, TextAlignmentOptions.Left);
            descTMP.enableWordWrapping = true;
            descTMP.margin = new Vector4(0, 0, 0, 16);
            descTMP.name = "PieceDescription";
        }

        /// <summary>
        /// Cria o botão de detalhes
        /// </summary>
        private void CreateDetailsButton(
            Transform parent,
            ComputerPiece piece,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var detailsBtn = createCTAButtonFunc(parent, "Ver detalhes →", () => OnPieceSelected?.Invoke(piece));

            var detailsBtnLE = detailsBtn.GetComponent<LayoutElement>();
            if (detailsBtnLE == null) detailsBtnLE = detailsBtn.AddComponent<LayoutElement>();
            detailsBtnLE.preferredHeight = buttonHeight;
            detailsBtnLE.minHeight = buttonHeight;

            detailsBtn.name = "DetailsButton";
        }

        /// <summary>
        /// Muda a ordem de classificação da timeline
        /// </summary>
        public void ChangeSortOrder(SortOrder newOrder)
        {
            if (_currentSortOrder != newOrder)
            {
                _currentSortOrder = newOrder;
                OnSortOrderChanged?.Invoke(newOrder);

                // Regenerate timeline if it exists
                if (_timelineContainer != null)
                {
                    // Get the creation functions - this would need to be passed in or stored
                    // For now, just sort and recreate manually
                    RefreshTimeline();
                }
            }
        }

        /// <summary>
        /// Atualiza a timeline existente sem recriar do zero
        /// </summary>
        public void RefreshTimeline()
        {
            if (_timelineContainer == null)
            {
                return;
            }

            SortPieces();

            if (_createGlassCardFunc == null || _createCTAButtonFunc == null)
            {
                return;
            }

            ClearTimeline();

            foreach (var piece in _sortedPieces)
            {
                CreateTimelineCard(_timelineContainer.transform, piece, _createGlassCardFunc, _createCTAButtonFunc);
            }

            OnTimelineGenerated?.Invoke(_sortedPieces);

            RefreshScrollMetrics();
        }

        /// <summary>
        /// Limpa a timeline existente
        /// </summary>
        public void ClearTimeline()
        {
            if (_timelineContainer != null)
            {
                for (int i = _timelineContainer.transform.childCount - 1; i >= 0; i--)
                {
                    var child = _timelineContainer.transform.GetChild(i);
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Filtra a timeline por período
        /// </summary>
        public void FilterByYearRange(int startYear, int endYear)
        {
            var pieces = SampleData.Pieces ?? new List<ComputerPiece>();
            _filteredPieces = pieces
                .Where(p => p.YearManufactured >= startYear && p.YearManufactured <= endYear)
                .ToList();
            RefreshTimeline();
        }

        /// <summary>
        /// Remove filtros e mostra todas as peças
        /// </summary>
        public void ClearFilters()
        {
            _filteredPieces = null;
            RefreshTimeline();
        }

        /// <summary>
        /// Busca por peças na timeline
        /// </summary>
        public void SearchPieces(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                ClearFilters();
                return;
            }

            searchTerm = searchTerm.ToLower();
            var pieces = SampleData.Pieces ?? new List<ComputerPiece>();
            _filteredPieces = pieces
                .Where(p =>
                {
                    var name = p.Name ?? string.Empty;
                    var description = p.Description ?? string.Empty;
                    var manufacturer = p.Manufacturer ?? string.Empty;
                    return name.ToLower().Contains(searchTerm) ||
                           description.ToLower().Contains(searchTerm) ||
                           manufacturer.ToLower().Contains(searchTerm);
                })
                .ToList();
            RefreshTimeline();
        }

        /// <summary>
        /// Define se a página está ativa ou não
        /// </summary>
        public void SetActive(bool active)
        {
            if (_pageObject != null)
                _pageObject.SetActive(active);
        }

        /// <summary>
        /// Obtém referência ao GameObject da página
        /// </summary>
        public GameObject GetPageObject()
        {
            return _pageObject;
        }

        /// <summary>
        /// Atualiza as configurações visuais da página
        /// </summary>
        public void UpdateVisualSettings(float cardHeight, float titleMargin, int titleSize, int yearSize,
                                       int nameSize, int descSize, int btnSize, float btnHeight)
        {
            timelineCardHeight = cardHeight;
            titleMarginBottom = titleMargin;
            pageTitleFontSize = titleSize;
            yearFontSize = yearSize;
            nameCardFontSize = nameSize;
            descriptionFontSize = descSize;
            buttonFontSize = btnSize;
            buttonHeight = btnHeight;
        }

        /// <summary>
        /// Obtém a ordem de classificação atual
        /// </summary>
        public SortOrder GetCurrentSortOrder()
        {
            return _currentSortOrder;
        }

        /// <summary>
        /// Obtém a lista de peças atualmente exibidas
        /// </summary>
        public List<ComputerPiece> GetDisplayedPieces()
        {
            var pieces = _sortedPieces ?? SampleData.Pieces ?? new List<ComputerPiece>();
            return new List<ComputerPiece>(pieces);
        }

        /// <summary>
        /// Obtém estatísticas da timeline
        /// </summary>
        public (int totalPieces, int earliestYear, int latestYear) GetTimelineStats()
        {
            var pieces = _sortedPieces ?? SampleData.Pieces;
            if (pieces.Count == 0)
                return (0, 0, 0);

            var years = pieces.Select(p => p.YearManufactured).Where(y => y > 0);
            return (pieces.Count, years.DefaultIfEmpty(0).Min(), years.DefaultIfEmpty(0).Max());
        }

        /// <summary>
        /// Utilitário para adicionar espaçador
        /// </summary>
        private void AddSpacer(Transform parent, float width, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            if (width > 0)
            {
                float responsiveWidth = ResponsiveTypography.ResponsiveSpacing(width);
                le.preferredWidth = responsiveWidth;
                le.minWidth = responsiveWidth;
            }

            if (height > 0)
            {
                float responsiveHeight = ResponsiveTypography.ResponsiveSpacing(height);
                le.preferredHeight = responsiveHeight;
                le.minHeight = responsiveHeight;
            }
        }

        /// <summary>
        /// Configura a superfície padrão para comportar o layout específico da timeline.
        /// Remove espaçadores flexíveis automáticos e garante que o ScrollRect aponte
        /// para o conteúdo correto.
        /// </summary>
        private void ConfigureSurfaceForTimeline(GameObject surface, RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            var prototypeFitter = content.GetComponent<PrototypeSurfaceContentFitter>();
            if (prototypeFitter != null)
            {
                Destroy(prototypeFitter);
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                if (child != null && (child.name == "TopFlexibleSpace" || child.name == "BottomFlexibleSpace"))
                {
                    Destroy(child.gameObject);
                }
            }

            var contentLayout = content.GetComponent<LayoutElement>();
            if (contentLayout != null)
            {
                contentLayout.flexibleHeight = 0f;
                contentLayout.preferredHeight = -1f;
                contentLayout.minHeight = 0f;
            }

            _scrollRect = surface != null ? surface.GetComponentInChildren<ScrollRect>() : null;
            if (_scrollRect == null)
            {
                Debug.LogWarning("TimelinePage: ScrollRect not found on surface. Timeline content might not scroll as expected.");
                return;
            }

            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.inertia = true;

            if (_scrollRect.viewport == null)
            {
                var viewport = _scrollRect.transform.Find("Viewport") as RectTransform;
                if (viewport != null)
                {
                    _scrollRect.viewport = viewport;
                }
            }

            if (_scrollRect.content == null || _scrollRect.content != content)
            {
                _scrollRect.content = content;
            }
        }

        /// <summary>
        /// Atualiza métricas do ScrollRect forçando o recálculo dos layouts
        /// depois que os elementos são adicionados dinamicamente.
        /// </summary>
        private void RefreshScrollMetrics()
        {
            if (_contentContainer == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer);

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            OnPieceSelected = null;
            OnTimelineGenerated = null;
            OnSortOrderChanged = null;
        }

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            titleMarginBottom = Mathf.Max(0, titleMarginBottom);
            timelineCardHeight = Mathf.Max(100f, timelineCardHeight);
            pageTitleFontSize = Mathf.Max(8, pageTitleFontSize);
            yearFontSize = Mathf.Max(8, yearFontSize);
            nameCardFontSize = Mathf.Max(8, nameCardFontSize);
            descriptionFontSize = Mathf.Max(8, descriptionFontSize);
            buttonFontSize = Mathf.Max(8, buttonFontSize);
            buttonHeight = Mathf.Max(20f, buttonHeight);
        }
#endif
        #endregion
    }
}