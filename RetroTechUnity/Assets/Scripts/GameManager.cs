using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Linq;
#if ZXING_PRESENT
using ZXing;
using ZXing.Common;
#endif

using static RetroTech.UiKit;

namespace RetroTech
{
    public class GameManager : MonoBehaviour
    {
        private TextMeshProUGUI _scoreLabel;
        private Image _progressFill;
        private GameObject _nextBtnGO;
        private TextMeshProUGUI _expTextTMP;

        private Canvas _canvas;
        private GameObject[] _pages;
        private GameObject _navBar;

        // Palette
        private readonly Color BackgroundColor = new Color32(23, 22, 35, 255);
        private readonly Color CardColor = new Color32(66, 49, 102, 255);
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);
        private readonly Color AccentColor = new Color32(200, 165, 220, 255);

        // Big gradient
        private readonly Color GradientTop = new Color32(120, 68, 180, 255);
        private readonly Color GradientBottom = new Color32(237, 129, 187, 255);
        private Sprite _fallbackGradient;

        // Icons (Resources/Icons/*.png)
        private Sprite _iconHome, _iconCategories, _iconTimeline, _iconScanner, _iconQuiz;

        private Dictionary<string, bool> _categoryExpanded = new();
        private int _currentQuizIndex;
        private int _quizScore;
        private int _activeTab = 0;
        private GameObject _openModal;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            var go = new GameObject("GameManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            _fallbackGradient = CreateFallbackGradient(GradientTop, GradientBottom);
            LoadIcons();
            CreateCanvas();

            CreatePages();
            CreateNavigationBar();
            SwitchPage(0);
        }

        // ========= Canvas & Input =========

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
                es.AddComponent<TouchInputModule>();
#endif
            }
        }

        private void LoadIcons()
        {
            _iconHome = Resources.Load<Sprite>("Icons/icon_home");
            _iconCategories = Resources.Load<Sprite>("Icons/icon_categories");
            _iconTimeline = Resources.Load<Sprite>("Icons/icon_timeline");
            _iconScanner = Resources.Load<Sprite>("Icons/icon_scanner");
            _iconQuiz = Resources.Load<Sprite>("Icons/icon_quiz");
        }

        private Sprite CreateFallbackGradient(Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(1, 2);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(new Color[] { bottom, top });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f));
        }

        // ========= Page factory =========

        private void CreatePages()
        {
            _pages = new GameObject[5];
            _pages[0] = CreateHomePage();
            _pages[1] = CreateCategoriesPage();
            _pages[2] = CreateTimelinePage();
            _pages[3] = CreateScannerPage();
            _pages[4] = CreateQuizPage();
        }

        /// <summary>Creates the rounded gradient "surface" and a content container inside it.</summary>
        private (Image surface, RectTransform content) BuildSurface(string name)
        {
            // Rounded gradient surface (MPUIKit if present; fallback uses sprite)
            var surface = UiKit.CreateCard(_canvas.transform, Vector2.zero, null, 28f, false, GradientTop, GradientBottom);
            var srt = surface.rectTransform;
            srt.anchorMin = new Vector2(0f, 0.08f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(12, 12);
            srt.offsetMax = new Vector2(-12, -12);

#if !MPUIKIT_PRESENT
            var img = surface.GetComponent<Image>();
            img.sprite = _fallbackGradient;
            img.type = Image.Type.Simple;
            img.color = Color.white;
#endif

            var page = new GameObject(name);
            page.transform.SetParent(surface.transform, false);
            var rt = page.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18, 18); rt.offsetMax = new Vector2(-18, -18);

            var vlg = page.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperLeft;

            return (surface, rt);
        }

        // ========= Navigation =========

        private void CreateNavigationBar()
        {
            _navBar = new GameObject("NavigationBar");
            _navBar.transform.SetParent(_canvas.transform, false);
            var rt = _navBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 76f);

            var bgGO = new GameObject("Bg");
            bgGO.transform.SetParent(_navBar.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(10, 8); bgRT.offsetMax = new Vector2(-10, 8);

            var bgImg = bgGO.AddComponent<Image>();
            bgImg.type = Image.Type.Sliced;
            bgImg.sprite = Resources.Load<Sprite>("Sprites/RoundedPanel"); // 9-slice rounded sprite (see note)
            bgImg.color = new Color(1f, 1f, 1f, 0.12f); // glass look
            var outline = bgGO.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.25f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Row for tabs
            var row = new GameObject("Row");
            row.transform.SetParent(bgGO.transform, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = Vector2.zero; rowRT.anchorMax = Vector2.one;
            rowRT.offsetMin = Vector2.zero; rowRT.offsetMax = Vector2.zero;

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 8f;
            h.childControlWidth = true; h.childForceExpandWidth = true;
            h.childControlHeight = true; h.childForceExpandHeight = true;
            h.padding = new RectOffset(16, 16, 8, 8);

            // Create tabs
            CreateNavTab(row.transform, "Início", 0, _iconHome);
            CreateNavTab(row.transform, "Categorias", 1, _iconCategories);
            CreateNavTab(row.transform, "Timeline", 2, _iconTimeline);
            CreateNavTab(row.transform, "QR Code", 3, _iconScanner);
            CreateNavTab(row.transform, "Quiz", 4, _iconQuiz);

            // Ensure on top
            _navBar.transform.SetAsLastSibling();

            // Respect safe area
            ApplySafeArea(rt);

            RefreshTabsVisual();
        }

    private void CreateNavTab(Transform parent, string label, int pageIndex, Sprite icon)
    {
        // Cell
        var cell = new GameObject("Tab_" + label);
        cell.transform.SetParent(parent, false);
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = 1; // even distribution

        // Hit area + background (transparent)
        var bg = cell.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0); // keep transparent; bg card is behind
        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg; // important: Button needs a targetGraphic
        btn.onClick.AddListener(() => { _activeTab = pageIndex; SwitchPage(pageIndex); RefreshTabsVisual(); });

        // Vertical content
        var v = cell.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 2f;
        v.childControlHeight = true; v.childForceExpandHeight = false;

        // Icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(cell.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = new Color32(245, 245, 255, 255);
        var iRT = iconGO.GetComponent<RectTransform>();
        iRT.sizeDelta = new Vector2(20, 20);

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(cell.transform, false);
        var t = labelGO.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 12;
        t.color = new Color32(210, 210, 235, 255);
        t.alignment = TextAnchor.MiddleCenter;
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(0, 16);
    }

        private void RefreshTabsVisual()
        {
            var row = _navBar.transform.Find("Bg/Row");
            if (!row) return;

            for (int i = 0; i < row.childCount; i++)
            {
                var tab = row.GetChild(i);
                bool active = (i == _activeTab);

                // Icon color
                var icon = tab.Find("Icon").GetComponent<Image>();
                icon.color = active ? new Color32(255, 255, 255, 255) : new Color32(220, 220, 240, 255);

                // Label color
                var text = tab.Find("Label").GetComponent<Text>();
                text.color = active ? new Color32(245, 245, 255, 255) : new Color32(210, 210, 235, 255);
            }
        }

        public void SwitchPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null) _pages[i].SetActive(i == index);

            _activeTab = index;
            RefreshTabsVisual();
        }

        // ========= Pages =========

        private GameObject CreateHomePage()
        {
            var (surface, content) = BuildSurface("HomePage");

            UiKit.TMP(content, "RetroTech", 60, TextMain, TextAlignmentOptions.Left, bold: true);
            UiKit.TMP(content, "Bem-vindo ao RetroTech", 40, TextMain, TextAlignmentOptions.Left, bold: true);

            UiKit.TMP(content,
                "Explore e aprenda sobre o acervo de peças de computação do Departamento de Sistemas e Computação (DSC) da FURB de forma interativa.",
                40, TextMuted, TextAlignmentOptions.Left);

            UiKit.TMP(content, "Objetivo do aplicativo", 40, TextMain, TextAlignmentOptions.Left, bold: true);
            UiKit.TMP(content,
                "Facilitar o acesso e a compreensão do acervo histórico de peças de computação do DSC, proporcionando uma experiência educativa e imersiva.",
                35, TextMuted, TextAlignmentOptions.Left);

            UiKit.TMP(content, "Principais funcionalidades", 40, TextMain, TextAlignmentOptions.Left, bold: true);
            UiKit.TMP(content,
                "• Navegação por categorias\n• Linha do tempo interativa\n• Leitura de QR Codes\n• Detalhes e curiosidades\n• Quiz educativo",
                35, TextMuted, TextAlignmentOptions.Left);

            UiKit.TMP(content, "Equipe de desenvolvimento", 40, TextMain, TextAlignmentOptions.Left, bold: true);
            UiKit.TMP(content,
                "Ricardo Berndt - Ciência da Computação\n\n" +
                "Orientador: Dalton Solano dos Reis\nSupervisor: Miguel A. Wistainater",
                35, TextMuted, TextAlignmentOptions.Left);

            CreateCTAButton(content.transform, "Buscar Peças", () =>
            {
                _activeTab = 1; 
                SwitchPage(_activeTab);
                RefreshTabsVisual();
            });

            return surface.gameObject;
        }


        private GameObject CreateCategoriesPage()
        {
            var (surface, content) = BuildSurface("CategoriesPage");

            //var title = UiKit.TMP(content, "RetroTech", 24, TextMain, TextAlignmentOptions.Left, bold: true);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(LayoutElement));
            fill.transform.SetParent(content, false);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var le = fill.GetComponent<LayoutElement>();
            le.flexibleHeight = 1; 

            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(fill.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(1, 1, 1, 0); // transparent
            var scroll = scrollGO.GetComponent<ScrollRect>(); scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            scroll.viewport = vpRt;

            var inner = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            inner.transform.SetParent(viewport.transform, false);
            var crt = inner.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            scroll.content = crt;

            var v = inner.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 24);
            v.spacing = 10;
            v.childControlWidth = true; v.childForceExpandWidth = true;
            v.childControlHeight = true; v.childForceExpandHeight = false;

            inner.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (Category cat in SampleData.Categories)
            {
                var (header, chevron) = CreateCategoryHeader(inner.transform, cat.Name);

                var subList = new GameObject($"SubList_{cat.Id}", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                subList.transform.SetParent(inner.transform, false);
                var subRT = subList.GetComponent<RectTransform>();
                subRT.anchorMin = new Vector2(0, 1); subRT.anchorMax = new Vector2(1, 1); subRT.pivot = new Vector2(0.5f, 1);

                var sv = subList.GetComponent<VerticalLayoutGroup>();
                sv.padding = new RectOffset(16, 8, 6, 6);
                sv.spacing = 6;
                sv.childControlWidth = true; sv.childForceExpandWidth = true;
                sv.childControlHeight = true; sv.childForceExpandHeight = false;
                subList.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                foreach (string sub in cat.Subcategories)
                {
                    var row = UiKit.CreateCard(subList.transform, new Vector2(0, 48),
                                               fill: new Color32(255, 255, 255, 40), radius: 14f, glass: true);
                    var rowLE = row.gameObject.AddComponent<LayoutElement>(); rowLE.minHeight = 48;

                    var btn = row.gameObject.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();

                    var txt = UiKit.TMP(row.transform, sub, 16, Color.black, TextAlignmentOptions.MidlineLeft);
                    txt.color = new Color(0, 0, 0, 0.9f);
                    txt.enableWordWrapping = false;
                    txt.overflowMode = TMPro.TextOverflowModes.Overflow;
                    txt.rectTransform.anchorMin = Vector2.zero;
                    txt.rectTransform.anchorMax = Vector2.one;
                    txt.rectTransform.offsetMin = new Vector2(16, 8);
                    txt.rectTransform.offsetMax = new Vector2(-16, -8);

                    string captured = sub;
                    btn.onClick.AddListener(() => ShowPiecesModal(cat.Id, captured));
                }

                if (!_categoryExpanded.ContainsKey(cat.Id)) _categoryExpanded[cat.Id] = false;
                subList.SetActive(_categoryExpanded[cat.Id]);
                chevron.localEulerAngles = _categoryExpanded[cat.Id] ? new Vector3(0, 0, 180) : Vector3.zero;

                string cid = cat.Id;
                var hBtn = header.GetComponent<Button>();
                hBtn.onClick.AddListener(() =>
                {
                    bool expanded = !_categoryExpanded[cid];
                    _categoryExpanded[cid] = expanded;
                    subList.SetActive(expanded);
                    chevron.localEulerAngles = expanded ? new Vector3(0, 0, 180) : Vector3.zero;
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
                });
            }

            return surface.gameObject;
        }

        private GameObject CreateTimelinePage()
        {
            var (surface, content) = BuildSurface("TimelinePage");

            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(content, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scroll.viewport = viewportRT;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var crt = contentGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            scroll.content = crt;

            var v = contentGO.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 24, 32);   
            v.spacing = 25;                               
            v.childForceExpandHeight = false;
            v.childControlHeight = true;
            v.childControlWidth = true;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var backLine = new GameObject("TimelineLine", typeof(RectTransform), typeof(Image));
            backLine.transform.SetParent(viewportGO.transform, false);
            backLine.transform.SetAsFirstSibling();
            var blrt = backLine.GetComponent<RectTransform>();
            blrt.anchorMin = new Vector2(0, 0); blrt.anchorMax = new Vector2(0, 1);
            blrt.pivot = new Vector2(0, 1);
            blrt.anchoredPosition = new Vector2(60, 0);  
            blrt.sizeDelta = new Vector2(5, 0);          
            backLine.GetComponent<Image>().color = new Color32(255, 255, 255, 90);

            var pieces = new List<ComputerPiece>(SampleData.Pieces);
            pieces.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured));

            foreach (var piece in pieces)
            {
                // ROW
                var row = new GameObject($"Row-{piece.YearManufactured}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(contentGO.transform, false);
                var rowHL = row.GetComponent<HorizontalLayoutGroup>();
                rowHL.spacing = 16;
                rowHL.childControlHeight = true;
                rowHL.childControlWidth = true;
                rowHL.childForceExpandHeight = false;
                rowHL.childForceExpandWidth = false;
                row.GetComponent<LayoutElement>().preferredHeight = 270; 

                // GUTTER (fixo)
                var gutter = new GameObject("Gutter", typeof(RectTransform), typeof(LayoutElement));
                gutter.transform.SetParent(row.transform, false);
                var gutterLE = gutter.GetComponent<LayoutElement>();
                gutterLE.minWidth = 112;                 
                gutterLE.preferredWidth = 112;

                var seg = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                seg.transform.SetParent(gutter.transform, false);
                var segRT = seg.GetComponent<RectTransform>();
                segRT.anchorMin = new Vector2(0, 0); segRT.anchorMax = new Vector2(0, 1);
                segRT.anchoredPosition = new Vector2(48, 0);
                segRT.sizeDelta = new Vector2(3, 0);
                seg.GetComponent<Image>().color = new Color32(255, 255, 255, 70);

                var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(gutter.transform, false);
                var dotRT = dot.GetComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(0, 0.5f); dotRT.anchorMax = new Vector2(0, 0.5f);
                dotRT.anchoredPosition = new Vector2(48, 0);
                dotRT.sizeDelta = new Vector2(16, 16);  
                var dotImg = dot.GetComponent<Image>();
                dotImg.color = TextMain;
                dotImg.raycastTarget = false;

                var card = UiKit.CreateCard(row.transform, new Vector2(0, 250), default, 30f, glass: true); 
                var cardLE = card.gameObject.AddComponent<LayoutElement>();
                cardLE.flexibleWidth = 1;
                cardLE.preferredHeight = 200;

                var iv = card.gameObject.AddComponent<VerticalLayoutGroup>();
                iv.padding = new RectOffset(16, 16, 16, 16);
                iv.spacing = 15;                          
                iv.childAlignment = TextAnchor.UpperLeft;
                iv.childControlHeight = true;
                iv.childControlWidth = true;
                iv.childForceExpandWidth = true;

                UiKit.TMP(card.transform, piece.YearManufactured.ToString(), 28, TextMain, bold: true);
                UiKit.TMP(card.transform, piece.Name, 28, TextMain, bold: true);
                UiKit.TMP(card.transform, $"{piece.Manufacturer} — {piece.Description}", 22, TextMuted);

                var btnCard = UiKit.CreateCard(card.transform, Vector2.zero, PrimaryColor, 22f);
                var btnLE = btnCard.gameObject.AddComponent<LayoutElement>();
                btnLE.minWidth = 250;     
                btnLE.minHeight = 70;     
                btnLE.flexibleWidth = 1;

                var btn = btnCard.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => ShowPieceDetail(piece));

                var label = UiKit.TMP(btnCard.transform, "Ver detalhes", 18, Color.white,
                                      TextAlignmentOptions.Center, bold: true);
                label.enableWordWrapping = false;
                label.enableAutoSizing = false;
                label.overflowMode = TextOverflowModes.Ellipsis;

                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(14, 8);
                lrt.offsetMax = new Vector2(-14, -8);
            }

            return surface.gameObject;
        }


        private GameObject CreateScannerPage()
        {
            var (surface, content) = BuildSurface("ScannerPage");

            var title = UiKit.TMP(content, "Scanner QR", 44, TextMain, TextAlignmentOptions.Left, bold: true);
            title.margin = new Vector4(8, 6, 8, 10);

            AddSpacer(content, 6);

            var hero = UiKit.CreateCard(content, new Vector2(0, 0), fill: new Color32(255, 255, 255, 28), radius: 22f, glass: true);
            var heroLE = hero.gameObject.AddComponent<LayoutElement>();
            heroLE.minHeight = 300;
            heroLE.preferredHeight = 300;

            var heroV = hero.gameObject.AddComponent<VerticalLayoutGroup>();
            heroV.childAlignment = TextAnchor.MiddleCenter;
            heroV.spacing = 10;
            heroV.padding = new RectOffset(16, 16, 16, 16);

            var preview = CreateFramedSquare(hero.transform, 200f, borderAlpha: 1f);
            CreateCenteredIcon(preview, "Sprites/qr_glyph", 84);

            var h1 = UiKit.TMP(hero.transform, "Scanner QR Code", 36, TextMain, TextAlignmentOptions.Center, bold: true);
            h1.enableWordWrapping = false;

            var desc = UiKit.TMP(hero.transform,
                "Aponte a câmera para o QR code de uma peça para ver seus detalhes.",
                28, TextMuted, TextAlignmentOptions.Center);
            desc.margin = new Vector4(10, 0, 10, 0);

            AddSpacer(content, 8);

            var start = UiKit.CreateCard(content, new Vector2(0, 46), fill: Color.white, radius: 22f);
            var startLE = start.gameObject.AddComponent<LayoutElement>();
            startLE.minHeight = 46; startLE.preferredHeight = 46;

            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(start.transform, false);
            var r = row.GetComponent<HorizontalLayoutGroup>();
            r.childAlignment = TextAnchor.MiddleCenter;
            r.spacing = 8;
            r.padding = new RectOffset(16, 16, 8, 8);
            r.childForceExpandWidth = false; r.childForceExpandHeight = false;

            var iGo = new GameObject("Bolt", typeof(RectTransform), typeof(Image));
            iGo.transform.SetParent(row.transform, false);
            var iRT = iGo.GetComponent<RectTransform>(); iRT.sizeDelta = new Vector2(16, 16);
            var iImg = iGo.GetComponent<Image>();
            iImg.sprite = Resources.Load<Sprite>("Sprites/bolt");
            iImg.color = new Color32(103, 80, 164, 255);

            var startLbl = UiKit.TMP(row.transform, "Iniciar Scanner", 30, new Color32(103, 80, 164, 255),
                                     TextAlignmentOptions.Center, bold: true);
            startLbl.enableWordWrapping = false;

            var startBtn = start.gameObject.AddComponent<Button>();
            startBtn.interactable = false;

            AddSpacer(content, 10);

            var tips = UiKit.CreateCard(content, new Vector2(0, 0), fill: new Color32(255, 255, 255, 28), radius: 18f, glass: true);
            var tipsLE = tips.gameObject.AddComponent<LayoutElement>();
            tipsLE.minHeight = 160; tipsLE.preferredHeight = 160;

            var tipsV = tips.gameObject.AddComponent<VerticalLayoutGroup>();
            tipsV.childAlignment = TextAnchor.UpperLeft;
            tipsV.spacing = 6;
            tipsV.padding = new RectOffset(16, 16, 14, 14);

            var tipsTitle = UiKit.TMP(tips.transform, "Dicas para escanear:", 30, TextMain, bold: true);
            tipsTitle.enableWordWrapping = false;

            var tipsTxt = UiKit.TMP(tips.transform,
                "• Mantenha o QR code bem iluminado\n" +
                "• Mantenha a câmera estável\n" +
                "• Certifique-se que o código esteja completo na tela",
                26, TextMuted, TextAlignmentOptions.Left);

            AddSpacer(content, 10);

            return surface.gameObject;
        }

        private GameObject CreateQuizPage()
        {
            var (surface, content) = BuildSurface("QuizPage");

            var header = UiKit.CreateCard(content, new Vector2(0, 48), new Color(1, 1, 1, 0.18f), 18f, glass: true);
            var h = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 8, 8);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;

            UiKit.TMP(header.transform, "Quiz RetroTech", 46, Color.white, TextAlignmentOptions.Left, bold: true)
                .enableWordWrapping = false;

            var scoreGO = new GameObject("Score", typeof(RectTransform));
            scoreGO.transform.SetParent(header.transform, false);
            var scoreLE = scoreGO.AddComponent<LayoutElement>(); scoreLE.flexibleWidth = 1;
            _scoreLabel = (TextMeshProUGUI)UiKit.TMP(header.transform, "Pontuação: 0", 34, Color.white, TextAlignmentOptions.Right);
            var progress = UiKit.CreateCard(content, new Vector2(0, 6), new Color(1, 1, 1, 0.22f), 8f, glass: true);
            var barBG = progress;
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barBG.transform, false);
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(2, -2);
            _progressFill = fillGO.GetComponent<Image>();
            _progressFill.color = new Color32(255, 255, 255, 240);
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillAmount = 0f;

            var qCard = UiKit.CreateCard(content, new Vector2(0, 84), new Color(1, 1, 1, 0.32f), 18f, glass: true);
            var qText = (TextMeshProUGUI)UiKit.TMP(qCard.transform, "", 36, Color.white, TextAlignmentOptions.Midline, bold: false);
            qText.enableWordWrapping = true;
            qText.margin = new Vector4(12, 10, 12, 10);
            qText.overflowMode = TMPro.TextOverflowModes.Overflow;

            var opts = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            opts.transform.SetParent(content, false);
            var ov = opts.GetComponent<VerticalLayoutGroup>();
            ov.spacing = 10; ov.padding = new RectOffset(0, 0, 6, 6);
            ov.childControlHeight = true; 
            ov.childForceExpandHeight = false;
            ov.childControlWidth = true;
            ov.childForceExpandWidth = true; 

            var expCard = UiKit.CreateCard(content, new Vector2(0, 120), new Color(1, 1, 1, 0.18f), 18f, glass: true);
            var expV = expCard.gameObject.AddComponent<VerticalLayoutGroup>();
            expV.padding = new RectOffset(14, 14, 12, 12); expV.spacing = 6;
            UiKit.TMP(expCard.transform, "❌  Incorreto!", 15, Color.white, TextAlignmentOptions.Left, bold: true)
                .name = "ExpTitle";
            _expTextTMP = (TextMeshProUGUI)UiKit.TMP(expCard.transform, "", 26, new Color32(255, 255, 255, 220), TextAlignmentOptions.Left);
            expCard.gameObject.SetActive(false);

            var next = UiKit.CreateCard(content, new Vector2(0, 46), Color.white, 22f);
            _nextBtnGO = next.gameObject;
            var nextBtn = next.gameObject.AddComponent<Button>();
            UiKit.TMP(next.transform, "Próxima Pergunta", 30, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true)
                 .enableWordWrapping = false;
            _nextBtnGO.SetActive(false);

            _currentQuizIndex = 0;
            _quizScore = 0;

            PopulateQuizQuestion(qText, opts, expCard.gameObject, _nextBtnGO);

            nextBtn.onClick.AddListener(() =>
            {
                _currentQuizIndex++;
                if (_currentQuizIndex >= SampleData.QuizQuestions.Count)
                    ShowQuizResult(content.gameObject);
                else
                    PopulateQuizQuestion(qText, opts, expCard.gameObject, _nextBtnGO);
            });

            return surface.gameObject;
        }

        // ========= Modals & Search (kept from your original, lightly styled where visible) =========
        // ... (keep your ShowPiecesModal, ShowPieceDetail exactly as before; they continue to work)

        private void ShowPiecesModal(string categoryId, string subcategoryName)
        {
            // Close any previous modal
            CloseModal();

            var root = FindObjectOfType<Canvas>().transform;

            // --- Overlay ------------------------------------------------------------
            var overlay = new GameObject("ModalOverlay", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            overlay.transform.SetParent(root, false);
            _openModal = overlay;

            var ovRt = overlay.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero; ovRt.anchorMax = Vector2.one; ovRt.offsetMin = Vector2.zero; ovRt.offsetMax = Vector2.zero;

            var ovImg = overlay.GetComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.55f);

            var ovBtn = overlay.GetComponent<Button>();
            ovBtn.transition = Selectable.Transition.None;
            ovBtn.onClick.AddListener(CloseModal);

            var cg = overlay.GetComponent<CanvasGroup>();
            cg.interactable = true; cg.blocksRaycasts = true; cg.ignoreParentGroups = false;

            // --- Panel --------------------------------------------------------------
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(overlay.transform, false);

            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.08f, 0.10f);
            prt.anchorMax = new Vector2(0.92f, 0.90f);
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            prt.pivot = new Vector2(0.5f, 0.5f);

            var pImg = panel.GetComponent<Image>();
            pImg.color = Color.white; // light panel
                                      // If you have a 9-sliced rounded sprite, set it here:
                                      // pImg.type = Image.Type.Sliced; pImg.sprite = RoundedSprite;

            var pV = panel.GetComponent<VerticalLayoutGroup>();
            pV.padding = new RectOffset(16, 16, 16, 16);
            pV.spacing = 12;
            pV.childControlWidth = true; pV.childForceExpandWidth = true;
            pV.childControlHeight = true; pV.childForceExpandHeight = false;

            // --- Header row ---------------------------------------------------------
            var header = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            header.transform.SetParent(panel.transform, false);
            var hrt = header.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1);
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            header.GetComponent<LayoutElement>().preferredHeight = 40;

            var title = UiKit.TMP(header.transform, $"{subcategoryName}", 22, Color.black, TextAlignmentOptions.MidlineLeft, bold: true);
            var trt = title.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(0, 0); trt.offsetMax = new Vector2(48, 0);
            title.enableWordWrapping = false; title.overflowMode = TextOverflowModes.Overflow; title.color = new Color(0, 0, 0, 0.95f);

            // Close button (top-right)
            var close = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            close.transform.SetParent(header.transform, false);
            var crtBtn = close.GetComponent<RectTransform>();
            crtBtn.anchorMin = new Vector2(1, 0.5f); crtBtn.anchorMax = new Vector2(1, 0.5f); crtBtn.pivot = new Vector2(1, 0.5f);
            crtBtn.sizeDelta = new Vector2(36, 36); crtBtn.anchoredPosition = Vector2.zero;

            var cImg = close.GetComponent<Image>(); cImg.color = new Color(0, 0, 0, 0.06f);
            var cBtn = close.GetComponent<Button>(); cBtn.onClick.AddListener(CloseModal);

            var xTxt = UiKit.TMP(close.transform, "✕", 22, Color.black, TextAlignmentOptions.Center, bold: true);
            xTxt.enableWordWrapping = false; xTxt.overflowMode = TextOverflowModes.Overflow;

            // --- Scroll view --------------------------------------------------------
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(panel.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1); srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.03f);
            var scroll = scrollGO.GetComponent<ScrollRect>(); scroll.vertical = true; scroll.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;
            scroll.viewport = vpRt;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contRt = content.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0, 1); contRt.anchorMax = new Vector2(1, 1);
            contRt.pivot = new Vector2(0.5f, 1); contRt.offsetMin = Vector2.zero; contRt.offsetMax = Vector2.zero;
            scroll.content = contRt;

            var v = content.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 8);
            v.spacing = 8;
            v.childControlWidth = true; v.childForceExpandWidth = true;
            v.childControlHeight = true; v.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            foreach (var piece in SampleData.Pieces)
            {
                var row = UiKit.CreateCard(content.transform, new Vector2(0, 56), null, 14f, glass: true);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                // Ensure row height for layout
                var le = row.gameObject.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 56; le.minHeight = 56;

                var btn = row.gameObject.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => ShowPieceDetail(piece));

                // Dark text on light card
                var label = UiKit.TMP(row.transform, piece.Name, 18, Color.black, TextAlignmentOptions.MidlineLeft, bold: false);
                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(16, 8); lrt.offsetMax = new Vector2(16, -8);
                label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Overflow; label.color = new Color(0, 0, 0, 0.92f);
            }

            // force layout so nothing stacks vertically letter-by-letter
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
        }

        private void CloseModal()
        {
            if (_openModal != null)
            {
                Destroy(_openModal);
                _openModal = null;
            }
        }

        private void ShowPieceDetail(ComputerPiece piece)
        {
            // Close any open modal
            if (_openModal != null) { Destroy(_openModal); _openModal = null; }

            // Root on app canvas and ensure on top
            var root = _canvas.transform;
            var overlay = new GameObject("PieceDetailOverlay",
                typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(root, false);
            overlay.transform.SetAsLastSibling();
            _openModal = overlay;

            // Fullscreen translucent backdrop
            var ovRt = overlay.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero; ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero; ovRt.offsetMax = Vector2.zero;
            var ovImg = overlay.GetComponent<Image>(); ovImg.color = new Color(0, 0, 0, 0.55f);
            overlay.GetComponent<Button>().onClick.AddListener(() => { if (_openModal) { Destroy(_openModal); _openModal = null; } });

            // Rounded panel
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(overlay.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.06f, 0.06f);
            prt.anchorMax = new Vector2(0.94f, 0.94f);
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            var pImg = panel.GetComponent<Image>();
            var rounded = Resources.Load<Sprite>("Sprites/RoundedPanel");  // 9-slice
            if (rounded) { pImg.sprite = rounded; pImg.type = Image.Type.Sliced; }
            pImg.color = Color.white;

            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 14, 14);
            vlg.spacing = 10;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

            // ---------- APP BAR ----------
            var appbar = new GameObject("AppBar", typeof(RectTransform), typeof(LayoutElement));
            appbar.transform.SetParent(panel.transform, false);
            appbar.GetComponent<LayoutElement>().preferredHeight = 40;
            var titleRow = UiKit.TMP(appbar.transform, "RetroTech", 18, Color.white, TMPro.TextAlignmentOptions.Left, bold: true);
            titleRow.enableWordWrapping = false;

            // back chevron + “Voltar”
            var back = new GameObject("Back", typeof(RectTransform), typeof(Button));
            back.transform.SetParent(appbar.transform, false);
            var brt = back.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f); brt.pivot = new Vector2(0, 0.5f);
            brt.sizeDelta = new Vector2(120, 36); brt.anchoredPosition = new Vector2(0, 0);
            var backBtn = back.GetComponent<Button>();
            var backTxt = UiKit.TMP(back.transform, "◀  Voltar", 14, new Color(1, 1, 1, 0.85f),
                                    TMPro.TextAlignmentOptions.MidlineLeft, bold: false);
            backBtn.onClick.AddListener(() => { if (_openModal) { Destroy(_openModal); _openModal = null; } });

            // ---------- SCROLL AREA (fills remaining space) ----------
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
            scrollGO.transform.SetParent(panel.transform, false);
            var sLE = scrollGO.GetComponent<LayoutElement>(); sLE.flexibleHeight = 1; sLE.minHeight = 100;

            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var scroll = scrollGO.GetComponent<ScrollRect>(); scroll.vertical = true; scroll.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0);  // transparent
            scroll.viewport = vpRt;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contRt = content.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0, 1); contRt.anchorMax = new Vector2(1, 1);
            contRt.pivot = new Vector2(0.5f, 1); contRt.offsetMin = Vector2.zero; contRt.offsetMax = Vector2.zero;
            scroll.content = contRt;

            var cv = content.GetComponent<VerticalLayoutGroup>();
            cv.padding = new RectOffset(8, 8, 8, 8);
            cv.spacing = 10;
            cv.childControlWidth = true; cv.childForceExpandWidth = true;
            cv.childControlHeight = true; cv.childForceExpandHeight = false;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ---------- HERO IMAGE CARD ----------
            var heroCard = UiKit.CreateCard(content.transform, new Vector2(0, 180), Color.white, 16f, glass: true);
            var hero = heroCard.gameObject.GetComponent<Image>();
            hero.color = new Color(1, 1, 1, 0.9f);
            var heroLE = heroCard.gameObject.GetComponent<LayoutElement>() ?? heroCard.gameObject.AddComponent<LayoutElement>();
            heroLE.minHeight = 160; heroLE.preferredHeight = 200;

            // optional sprite
            if (!string.IsNullOrEmpty(piece.ImageUrl))
            {
                var sp = Resources.Load<Sprite>(piece.ImageUrl);
                if (sp) heroCard.sprite = sp;
            }

            // ---------- METADATA ----------
            AddMutedLabel("Ano de fabricação");
            AddValue(piece.YearManufactured > 0 ? piece.YearManufactured.ToString() : "-");

            AddMutedLabel("Fabricante");
            AddValue(string.IsNullOrEmpty(piece.Manufacturer) ? (piece.Category ?? "-") : piece.Manufacturer);

            // ---------- DESCRIPTION ----------
            AddSectionTitle("A primeira calculadora científica portátil do mundo, revolucionando os cálculos de engenharia.");
            if (!string.IsNullOrEmpty(piece.Description))
            {
                AddParagraph(piece.Description);
            }

            // ---------- CURIOSIDADES ----------
            AddSectionHeader("Curiosidades");
            AddParagraph(piece.Curiosities ??
                "A HP-35 foi chamada de “slide rule killer” porque substituiu as réguas de cálculo usadas por engenheiros.");

            // ---------- ESPECIFICAÇÕES (bulleted) ----------
            AddSectionHeader("Especificações");
            foreach (var item in piece.Specifications)
            {
                AddBullet(item);
            }

            // bottom spacer so the CTA doesn’t cover text
            var spacer = new GameObject("Spacer", typeof(LayoutElement));
            spacer.transform.SetParent(content.transform, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = 70;

            // ---------- BOTTOM CTA (fixed) ----------
            var cta = UiKit.CreateCard(panel.transform, new Vector2(0, 46), Color.white, 22f);
            var ctaLE = cta.gameObject.AddComponent<LayoutElement>(); ctaLE.preferredHeight = 50;
            var ctaBtn = cta.gameObject.AddComponent<Button>();
            UiKit.TMP(cta.transform, "◧  Gerar QR Code desta peça", 14, new Color(0.38f, 0.31f, 0.64f, 1),
                      TMPro.TextAlignmentOptions.Center, bold: true).enableWordWrapping = false;
            // TODO: hook your generator here
            ctaBtn.onClick.AddListener(() => Debug.Log("Gerar QR para: " + piece.Name));

            // final layout pass
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(prt);

            // ---------- local UI builders ----------
            void AddMutedLabel(string text)
                => UiKit.TMP(content.transform, text, 12, new Color(1, 1, 1, 0.75f),
                             TMPro.TextAlignmentOptions.Left, bold: true);

            void AddValue(string text)
                => UiKit.TMP(content.transform, text, 14, Color.white,
                             TMPro.TextAlignmentOptions.Left);

            void AddSectionHeader(string text)
                => UiKit.TMP(content.transform, text, 14, Color.white,
                             TMPro.TextAlignmentOptions.Left, bold: true);

            void AddSectionTitle(string text)
                => UiKit.TMP(content.transform, text, 14, Color.white,
                             TMPro.TextAlignmentOptions.Left);

            void AddParagraph(string text)
            {
                var p = UiKit.TMP(content.transform, text, 13, new Color(1, 1, 1, 0.92f),
                                  TMPro.TextAlignmentOptions.Left);
                p.enableWordWrapping = true;
            }

            void AddBullet(string text)
                => UiKit.TMP(content.transform, "• " + text, 13, new Color(1, 1, 1, 0.92f),
                             TMPro.TextAlignmentOptions.Left);
        }

        private void ShowQRModal(ComputerPiece piece)
        {
            var modal = new GameObject("QRModal");
            modal.transform.SetParent(_canvas.transform, false);
            var rt = modal.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            var bg = modal.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.8f);

            var card = UiKit.CreateCard(modal.transform, Vector2.zero, default, 18f, glass: true);
            var crt = card.rectTransform; crt.anchorMin = new Vector2(0.2f, 0.3f); crt.anchorMax = new Vector2(0.8f, 0.7f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(20, 20, 20, 20); v.spacing = 10;
            UiKit.TMP(card.transform, "URL: https://retro.tech/piece/" + piece.Id, 16, TextMain, TextAlignmentOptions.Center);

            var close = UiKit.CreateCard(card.transform, new Vector2(0, 40), PrimaryColor, 18f);
            var btn = close.gameObject.AddComponent<Button>();
            UiKit.TMP(close.transform, "Fechar", 16, Color.white, TextAlignmentOptions.Center, bold:true);
            btn.onClick.AddListener(() => Destroy(modal));
        }

        // ========= Search =========

        private void CreateOrShowSearchPage()
        {
            if (_pages.Length < 6)
            {
                var newPages = new List<GameObject>(_pages);
                var searchPage = CreateSearchPage();
                newPages.Add(searchPage);
                _pages = newPages.ToArray();
                // add extra nav tab
                AddSearchNavTab();
            }
            SwitchPage(_pages.Length - 1);
        }

        private void AddSearchNavTab()
        {
            // Append a new button "Pesquisa"
            var row = _navBar.transform.GetChild(0).GetChild(0);
            CreateNavTab(row, "Pesquisa", _pages.Length - 1, _iconHome);
            RefreshTabsVisual();
        }

        private GameObject CreateSearchPage()
        {
            var (surface, content) = BuildSurface("SearchPage");

            // input
            var fieldCard = UiKit.CreateCard(content, new Vector2(0, 44), default, 16f, glass: true);
            var input = fieldCard.gameObject.AddComponent<InputField>();
            var iImg = fieldCard; iImg.color = new Color(1, 1, 1, 0.08f);
            var placeholderGO = new GameObject("Placeholder"); placeholderGO.transform.SetParent(fieldCard.transform, false);
            var ph = placeholderGO.AddComponent<Text>(); ph.text = "Buscar por nome..."; ph.color = new Color(0.7f, 0.7f, 0.8f, 1); ph.alignment = TextAnchor.MiddleLeft;
            var phrt = placeholderGO.GetComponent<RectTransform>(); phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one; phrt.offsetMin = new Vector2(10, 0); phrt.offsetMax = new Vector2(-10, 0);
            var textGO = new GameObject("Text"); textGO.transform.SetParent(fieldCard.transform, false);
            var t = textGO.AddComponent<Text>(); t.color = new Color(0.9f, 0.9f, 0.95f, 1); t.alignment = TextAnchor.MiddleLeft;
            var trt = textGO.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(10, 0); trt.offsetMax = new Vector2(-10, 0);
            input.textComponent = t; input.placeholder = ph;

            // sort
            var sortGO = UiKit.CreateCard(content, new Vector2(0, 44), default, 16f, glass: true);
            var sort = sortGO.gameObject.AddComponent<Dropdown>();
            sort.options = new List<Dropdown.OptionData> {
                new("Alfabético"), new("Ano crescente"), new("Ano decrescente")
            };

            // button
            var searchBtnCard = UiKit.CreateCard(content, new Vector2(0, 44), PrimaryColor, 18f);
            var searchBtn = searchBtnCard.gameObject.AddComponent<Button>();
#if TMP_PRESENT
            UiKit.TMP(searchBtnCard.transform, "Buscar", 16, Color.white, TextAlignmentOptions.Center, bold:true);
#endif

            // results
            var resultsGO = new GameObject("Results"); resultsGO.transform.SetParent(content, false);
            var rrt = resultsGO.AddComponent<RectTransform>(); rrt.anchorMin = new Vector2(0, 0); rrt.anchorMax = new Vector2(1, 1);
            var results = resultsGO.AddComponent<ScrollRect>(); results.vertical = true; results.horizontal = false;
            var resBg = resultsGO.AddComponent<Image>(); resBg.color = new Color(1, 1, 1, 0.06f);
            var resContent = new GameObject("Content"); resContent.transform.SetParent(resultsGO.transform, false);
            var rcrt = resContent.AddComponent<RectTransform>(); rcrt.anchorMin = new Vector2(0, 1); rcrt.anchorMax = new Vector2(1, 1); rcrt.pivot = new Vector2(0.5f, 1);
            results.content = rcrt;
            var resVlg = resContent.AddComponent<VerticalLayoutGroup>(); resVlg.padding = new RectOffset(10, 10, 10, 10); resVlg.spacing = 6; resVlg.childForceExpandHeight = false;

            searchBtn.onClick.AddListener(() =>
            {
                string q = input.text ?? "";
                var filtered = new List<ComputerPiece>();
                foreach (var p in SampleData.Pieces)
                    if (string.IsNullOrEmpty(q) || p.Name.ToLower().Contains(q.ToLower()))
                        filtered.Add(p);

                switch (sort.value)
                {
                    case 0: filtered.Sort((a, b) => a.Name.CompareTo(b.Name)); break;
                    case 1: filtered.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured)); break;
                    default: filtered.Sort((a, b) => b.YearManufactured.CompareTo(a.YearManufactured)); break;
                }

                foreach (Transform c in resContent.transform) Destroy(c.gameObject);
                foreach (var p in filtered)
                {
                    var row = UiKit.CreateCard(resContent.transform, new Vector2(0, 48), default, 14f, glass: true);
                    var btn = row.gameObject.AddComponent<Button>();
#if TMP_PRESENT
                    UiKit.TMP(row.transform, $"{p.Name} - {p.Description}", 14, TextMain);
#endif
                    var captured = p;
                    btn.onClick.AddListener(() => ShowPieceDetail(captured));
                }
            });

            return surface.gameObject;
        }

        // ========= Quiz helpers (reusing your logic, styled) =========
        private void PopulateQuizQuestion(TextMeshProUGUI questionTMP, GameObject optionsContainer, GameObject explanationGO, GameObject nextGO)
        {
            foreach (Transform child in optionsContainer.transform) Destroy(child.gameObject);
            explanationGO.SetActive(false); nextGO.SetActive(false);

            var q = SampleData.QuizQuestions[_currentQuizIndex];

            _scoreLabel.text = $"Pontuação: {_quizScore}";
            _progressFill.fillAmount = (_currentQuizIndex) / Mathf.Max(1f, (float)(SampleData.QuizQuestions.Count - 1));

            questionTMP.fontSize = 24;
            questionTMP.text = q.Question;

            for (int i = 0; i < q.Options.Count; i++)
            {
                int idx = i;

                var card = UiKit.CreateCard(optionsContainer.transform, new Vector2(0, 54), new Color(1, 1, 1, 0.88f), 16f);
                card.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
                var btn = card.gameObject.AddComponent<Button>();

                var lbl = UiKit.TMP(card.transform, q.Options[i], 16, new Color32(50, 50, 70, 255), TextAlignmentOptions.Center, bold: true);
                lbl.enableWordWrapping = false;

                btn.onClick.AddListener(() =>
                {
                    bool correct = idx == q.CorrectAnswerIndex;
                    if (correct) _quizScore++;

                    var expTitle = explanationGO.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    expTitle.text = correct ? "✅  Correto!" : "❌  Incorreto!";
                    expTitle.color = correct ? new Color32(210, 255, 220, 255) : new Color32(255, 220, 220, 255);

                    _expTextTMP.text = q.Explanation;
                    explanationGO.SetActive(true);

                    for (int c = 0; c < optionsContainer.transform.childCount; c++)
                    {
                        var opt = optionsContainer.transform.GetChild(c);
                        var img = opt.GetComponent<Image>();
                        var t = opt.GetComponentInChildren<TextMeshProUGUI>();
                        var b = opt.GetComponent<Button>();
                        if (b) b.interactable = false;

                        if (c == q.CorrectAnswerIndex) { img.color = new Color32(76, 175, 80, 255); t.color = Color.white; }
                        else if (c == idx && !correct) { img.color = new Color32(229, 57, 53, 255); t.color = Color.white; }
                        else { img.color = new Color(1, 1, 1, 0.28f); t.color = new Color32(210, 210, 220, 255); }
                    }

                    _scoreLabel.text = $"Pontuação: {_quizScore}";
                    nextGO.SetActive(true);
                });
            }
        }

        private void ShowQuizResult(GameObject quizPageContent)
        {
            foreach (Transform c in quizPageContent.transform) Destroy(c.gameObject);

            UiKit.TMP(quizPageContent.transform,
                $"Você acertou {_quizScore} de {SampleData.QuizQuestions.Count} perguntas!",
                20, Color.white, TextAlignmentOptions.Center, bold: true);

            var msg = (_quizScore == SampleData.QuizQuestions.Count)
                ? "Excelente! Você é um expert em tecnologia retrô."
                : (_quizScore >= SampleData.QuizQuestions.Count / 2)
                    ? "Muito bom! Continue explorando para aprender mais."
                    : "Você pode melhorar! Que tal estudar mais sobre as peças?";
            UiKit.TMP(quizPageContent.transform, msg, 15, new Color32(255, 230, 220, 255), TextAlignmentOptions.Center);

            // save high score
            int previousHigh = PlayerPrefs.GetInt("RetroTech_HighScore", 0);
            if (_quizScore > previousHigh) { PlayerPrefs.SetInt("RetroTech_HighScore", _quizScore); PlayerPrefs.Save(); }

            // restart button
            var restart = UiKit.CreateCard(quizPageContent.transform, new Vector2(0, 46), Color.white, 22f);
            var btn = restart.gameObject.AddComponent<Button>();
            UiKit.TMP(restart.transform, "Reiniciar Quiz", 15, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true)
                 .enableWordWrapping = false;

            btn.onClick.AddListener(() =>
            {
                foreach (Transform c in quizPageContent.transform) Destroy(c.gameObject);
                // rebuild page
                var (surface, content) = (null as Image, quizPageContent.GetComponent<RectTransform>()); // reuse container
                CreateQuizPage(); // simplest: rebuild via original method to restore header/progress etc.
            });
        }

        // ========= Scanner =========

        private void SimulateScan()
        {
#if UNITY_ANDROID
            try
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            } catch {}
#endif

#if ZXING_PRESENT
            StartCoroutine(ScanQRCode());
#else
            if (SampleData.Pieces.Count == 0) return;
            int index = Random.Range(0, SampleData.Pieces.Count);
            ShowPieceDetail(SampleData.Pieces[index]);
#endif
        }

#if ZXING_PRESENT
        private System.Collections.IEnumerator ScanQRCode()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                if (SampleData.Pieces.Count > 0)
                    ShowPieceDetail(SampleData.Pieces[Random.Range(0, SampleData.Pieces.Count)]);
                yield break;
            }
            var device = WebCamTexture.devices[0];
            var webcam = new WebCamTexture(device.name);
            webcam.Play();
            yield return new WaitForSeconds(0.5f);

            var reader = new BarcodeReader { AutoRotate = true, Options = new DecodingOptions { TryHarder = true } };
            ComputerPiece found = null;

            for (int attempt = 0; attempt < 30 && found == null; attempt++)
            {
                try
                {
                    var result = reader.Decode(webcam.GetPixels32(), webcam.width, webcam.height);
                    if (result != null)
                    {
                        string payload = result.Text;
                        const string prefix = "https://retro.tech/piece/";
                        if (payload.StartsWith(prefix)) payload = payload.Substring(prefix.Length);
                        foreach (var piece in SampleData.Pieces)
                            if (piece.Id == payload) { found = piece; break; }
                    }
                } catch {}
                yield return null;
            }
            webcam.Stop();

            if (found != null) ShowPieceDetail(found);
            else if (SampleData.Pieces.Count > 0) ShowPieceDetail(SampleData.Pieces[Random.Range(0, SampleData.Pieces.Count)]);
        }
#endif

        private void ApplySafeArea(RectTransform rt, int extraBottom = 8, int side = 10)
        {
            var sa = Screen.safeArea;
            Vector2 anchorMin = sa.position;
            Vector2 anchorMax = sa.position + sa.size;
            var canvas = _canvas.GetComponent<RectTransform>();
            anchorMin.x /= canvas.rect.width;
            anchorMin.y /= canvas.rect.height;
            anchorMax.x /= canvas.rect.width;
            anchorMax.y /= canvas.rect.height;

            var hlg = rt.GetComponentInChildren<HorizontalLayoutGroup>(true);
            if (hlg != null)
            {
                int bottomPad = Mathf.RoundToInt((1f - anchorMin.y) * 0f); 
                hlg.padding = new RectOffset(side, side, 8, extraBottom);
            }
        }

        private void CreateCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("CTA_BuscarPecas");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            img.sprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color32(103, 80, 164, 255);

            var lrt = tmp.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        }

        private (GameObject header, RectTransform chevron) CreateCategoryHeader(Transform parent, string title)
        {
            var headerImg = UiKit.CreateCard(parent, new Vector2(0, 56), fill: new Color32(255, 255, 255, 38), radius: 16f, glass: true);
            var header = headerImg.gameObject;

            var btn = header.GetComponent<Button>() ?? header.AddComponent<Button>();

            var ttl = UiKit.TMP(header.transform, title, 18, Color.black, TextAlignmentOptions.MidlineLeft, bold: true);
            ttl.color = new Color(0, 0, 0, 0.9f);
            ttl.enableWordWrapping = false;
            ttl.overflowMode = TMPro.TextOverflowModes.Overflow;
            ttl.rectTransform.anchorMin = new Vector2(0, 0);
            ttl.rectTransform.anchorMax = new Vector2(1, 1);
            ttl.rectTransform.offsetMin = new Vector2(16, 8);
            ttl.rectTransform.offsetMax = new Vector2(-44, -8); 

            var chev = CreateDisclosureIcon(header.transform);
            return (header, chev);
        }

        private RectTransform CreateDisclosureIcon(Transform parent)
        {
            var go = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(18, 18);
            rt.anchoredPosition = new Vector2(-14, 0);

            var img = go.GetComponent<Image>();
            var sp = Resources.Load<Sprite>("Sprites/Chevron");
            if (sp == null) Debug.LogWarning("Sprite 'Sprites/Chevron' não encontrado (Resources).");
            img.sprite = sp;
            img.color = new Color32(120, 100, 170, 255); 

            return rt;
        }



        #region HELPERS
            private void AddSpacer(Transform parent, float height)
            {
                var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                var le = go.GetComponent<LayoutElement>();
                le.minHeight = height; le.preferredHeight = height;
            }
            
            private RectTransform CreateFramedSquare(Transform parent, float size, float borderAlpha = 1f)
            {
                var holder = new GameObject("PreviewHolder", typeof(RectTransform));
                holder.transform.SetParent(parent, false);
                var hrt = holder.GetComponent<RectTransform>();
                hrt.sizeDelta = new Vector2(size, size);
            
                var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
                frame.transform.SetParent(holder.transform, false);
                var frt = frame.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            
                var img = frame.GetComponent<Image>();
                img.sprite = Resources.Load<Sprite>("Sprites/RoundedPanel"); 
                img.type = Image.Type.Sliced;
                img.color = new Color(1,1,1,borderAlpha);
                img.fillCenter = false; 
            
                var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(holder.transform, false);
                var irt = inner.GetComponent<RectTransform>();
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(8,8); irt.offsetMax = new Vector2(-8,-8);
                var bg = inner.GetComponent<Image>(); bg.color = new Color(1,1,1, 0.06f);
            
                return irt;
            }
            
            private void CreateCenteredIcon(RectTransform parent, string spritePath, float size)
            {
                var go = new GameObject("CenterIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(size, size);
                var img = go.GetComponent<Image>(); img.sprite = Resources.Load<Sprite>(spritePath); img.color = Color.white;
            }
            
        #endregion

    }
}
