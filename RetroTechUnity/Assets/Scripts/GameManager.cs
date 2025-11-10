using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

using static RetroTech.UiKit;
using RetroTech.Services;

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

        private float _navBarReservedHeight;

        // Updated Palette to match prototype
        private readonly Color BackgroundColor = new Color32(23, 22, 35, 255);
        private readonly Color CardColor = new Color32(255, 255, 255, 30); // Glass effect
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);
        private readonly Color AccentColor = new Color32(255, 255, 255, 255);

        // Beautiful gradient colors from prototype
        private readonly Color GradientTop = new Color32(147, 112, 219, 255);    // Light purple
        private readonly Color GradientBottom = new Color32(255, 182, 193, 255); // Light pink
        // Navigation bar reuses the same family of colors as the page gradient so it blends seamlessly
        private readonly Color NavGradientTop = new Color32(249, 197, 228, 255);   // Soft pink pulled from the page background
        private readonly Color NavGradientBottom = new Color32(142, 70, 199, 255); // Deep violet matching the footer of the page gradient
        private readonly Color IconBackgroundActiveColor = new Color(1f, 1f, 1f, 1f);
        private readonly Color IconBackgroundInactiveColor = new Color(1f, 1f, 1f, 0.82f);
        private readonly Color IconActiveColor = new Color(1f, 1f, 1f, 1f);
        private readonly Color IconInactiveColor = new Color(1f, 1f, 1f, 0.85f);
        private readonly Color NavIndicatorColor = new Color32(242, 210, 255, 255);
        private Sprite _fallbackGradient;
        private Sprite _navBarGradient;
        private Sprite _navIconGlow;
        private const float NavSidePadding = 14f;
        private const float NavTopPadding = 8f;
        private const float IconFrameSizeInactive = 68f;
        private const float IconFrameSizeActive = 80f;
        private const float IconGlyphSizeInactive = 30f;
        private const float IconGlyphSizeActive = 34f;

        // Icons (Resources/Icons/*.png)
        private Sprite _iconHome, _iconCategories, _iconTimeline, _iconScanner, _iconQuiz;

        private ApiConfiguration _apiConfiguration;
        private ApiClient _apiClient;
        private IContentService _contentService;

        private Dictionary<string, bool> _categoryExpanded = new();
        private int _activeTab = 0;
        private GameObject _openModal;

        private HomePage _homePage;
        private ScannerPage _scannerPage;
        private CategoriesPage _categoriesPage;
        private QuizPage _quizPage;

        private LoginPage _loginPage;
        private IAuthenticationService _authenticationService;
        private UserProfile _currentUser;
        private bool _contentInitialized;

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
            _navBarGradient = CreateVerticalGradient(NavGradientTop, NavGradientBottom);
            _navIconGlow = CreateRadialGlowSprite(NavGradientTop, NavGradientBottom);
            LoadIcons();
            CreateCanvas();
            SetupBackground();

            _navBarReservedHeight = NavBarHeight + Screen.safeArea.y;
            InitializeServices();
        }

        private async void Start()
        {
            try
            {
                CreateLoginPage();

                if (_authenticationService != null && _authenticationService.TryAutoSignIn(out var profile))
                {
                    await HandleAuthenticatedAsync(profile);
                }
                else
                {
                    _loginPage?.ResetState();
                    _loginPage?.ShowLogin();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize RetroTech content. {ex}");
                if (_loginPage != null)
                {
                    _loginPage.SetBusy(false);
                    _loginPage.ShowMessage("Não foi possível iniciar o aplicativo. Tente novamente.", true);
                    _loginPage.ShowLogin();
                }
            }
        }

        private void InitializeServices()
        {
            try
            {
                _apiConfiguration = ApiConfiguration.Load();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load API configuration. {ex}");
                _apiConfiguration = null;
            }

            if (_apiConfiguration == null)
            {
                return;
            }

            try
            {
                _authenticationService = new AuthenticationService(_apiConfiguration);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize authentication service. {ex}");
                _authenticationService = null;
            }

            try
            {
                _apiClient = new ApiClient(_apiConfiguration);
                _contentService = new ContentService(_apiClient, _apiConfiguration);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to configure content services. {ex}");
                _apiClient = null;
                _contentService = null;
            }
        }

        private async Task InitializeContentAsync()
        {
            await SampleData.InitializeAsync(_contentService);
        }

        // ========= Authentication =========

        private void CreateLoginPage()
        {
            if (_loginPage != null)
            {
                return;
            }

            var loginPageContainer = new GameObject("LoginPageContainer");
            loginPageContainer.transform.SetParent(transform, false);
            _loginPage = loginPageContainer.AddComponent<LoginPage>();
            var loginSurface = _loginPage.CreatePage(_canvas.transform, BuildPrototypeSurface);
            loginSurface.transform.SetAsLastSibling();

            _loginPage.OnLoginRequested += HandleLoginRequested;
            _loginPage.OnRegisterRequested += HandleRegisterRequested;
            _loginPage.ResetState();
        }

        private async void HandleLoginRequested(string username, string password)
        {
            if (_authenticationService == null)
            {
                _loginPage?.ShowMessage("Serviço de autenticação indisponível.", true);
                return;
            }

            _loginPage?.SetBusy(true);
            _loginPage?.ShowMessage(string.Empty, false);

            try
            {
                var result = await _authenticationService.SignInAsync(username, password);
                if (result.Succeeded)
                {
                    await HandleAuthenticatedAsync(result.Profile);
                }
                else
                {
                    _loginPage?.SetBusy(false);
                    _loginPage?.ClearPasswords();
                    _loginPage?.ShowMessage(result.ErrorMessage, true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during sign-in. {ex}");
                _loginPage?.SetBusy(false);
                _loginPage?.ClearPasswords();
                _loginPage?.ShowMessage("Não foi possível concluir o login. Tente novamente.", true);
            }
        }

        private async void HandleRegisterRequested(string username, string password)
        {
            if (_authenticationService == null)
            {
                _loginPage?.ShowMessage("Serviço de autenticação indisponível.", true);
                return;
            }

            _loginPage?.SetBusy(true);
            _loginPage?.ShowMessage(string.Empty, false);

            try
            {
                var result = await _authenticationService.RegisterAsync(username, password);
                if (result.Succeeded)
                {
                    await HandleAuthenticatedAsync(result.Profile);
                }
                else
                {
                    _loginPage?.SetBusy(false);
                    _loginPage?.ClearPasswords();
                    _loginPage?.ShowMessage(result.ErrorMessage, true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during registration. {ex}");
                _loginPage?.SetBusy(false);
                _loginPage?.ClearPasswords();
                _loginPage?.ShowMessage("Não foi possível concluir o cadastro. Tente novamente.", true);
            }
        }

        private async Task HandleAuthenticatedAsync(UserProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("Received a null profile after authentication.");
                _loginPage?.SetBusy(false);
                _loginPage?.ShowMessage("Não foi possível autenticar o usuário. Tente novamente.", true);
                _loginPage?.ShowLogin();
                return;
            }

            _currentUser = profile;

            if (!string.IsNullOrWhiteSpace(profile?.AccessToken))
            {
                if (_apiClient != null)
                {
                    _apiClient.SetBearerToken(profile.AccessToken);
                }
                else
                {
                    Debug.LogWarning("API client is not available to receive the authentication token.");
                }
            }
            else
            {
                Debug.LogWarning("Authenticated profile did not contain a valid access token.");
            }

            try
            {
                await EnsureContentInitializedAsync();
                _loginPage?.ClearPasswords();
                _loginPage?.SetBusy(false);
                _loginPage?.ShowMessage(string.Empty, false);
                _loginPage?.Hide();

                _activeTab = 0;
                SwitchPage(_activeTab);
            }
            catch (System.UnauthorizedAccessException authEx)
            {
                Debug.LogWarning($"Content loading requires authentication. {authEx}");
                _authenticationService?.SignOut();
                _apiClient?.SetBearerToken(null);
                _currentUser = null;

                _loginPage?.SetBusy(false);
                _loginPage?.ClearPasswords();
                _loginPage?.ShowMessage("Sua sessão expirou. Faça login novamente.", true);
                _loginPage?.ShowLogin();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load content after authentication. {ex}");
                _loginPage?.SetBusy(false);
                _loginPage?.ShowMessage("Não foi possível carregar o conteúdo. Tente novamente.", true);
                _loginPage?.ShowLogin();
            }
        }

        private async Task EnsureContentInitializedAsync()
        {
            if (_contentInitialized)
            {
                if (_pages == null || _pages.Length == 0)
                {
                    CreatePages();
                }

                if (_navBar == null)
                {
                    CreateNavigationBar();
                }

                return;
            }

            await InitializeContentAsync();
            CreatePages();
            CreateNavigationBar();
            _contentInitialized = true;
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

        private void SetupBackground()
        {
            // Create fullscreen background with gradient
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_canvas.transform, false);

            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = _fallbackGradient;
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(1f, 1f, 1f, 0.94f);

            bgGO.transform.SetAsFirstSibling();
        }

        private void LoadIcons()
        {
            _iconHome = Resources.Load<Sprite>("Icons/icon_home");
            _iconCategories = Resources.Load<Sprite>("Icons/icon_categories");
            _iconTimeline = Resources.Load<Sprite>("Icons/icon_timeline");
            _iconScanner = Resources.Load<Sprite>("Icons/icon_scanner");
            _iconQuiz = Resources.Load<Sprite>("Icons/icon_quiz");

            List<string> missingIcons = null;
            if (_iconHome == null) missingIcons = AppendMissing(missingIcons, "icon_home");
            if (_iconCategories == null) missingIcons = AppendMissing(missingIcons, "icon_categories");
            if (_iconTimeline == null) missingIcons = AppendMissing(missingIcons, "icon_timeline");
            if (_iconScanner == null) missingIcons = AppendMissing(missingIcons, "icon_scanner");
            if (_iconQuiz == null) missingIcons = AppendMissing(missingIcons, "icon_quiz");

            if (missingIcons != null)
            {
                Debug.LogWarning($"Navigation icons not found in Resources/Icons: {string.Join(", ", missingIcons)}");
            }
        }

        private static List<string> AppendMissing(List<string> list, string value)
        {
            list ??= new List<string>();
            list.Add(value);
            return list;
        }

        private Sprite CreateFallbackGradient(Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(1, 64);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[64];
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                colors[i] = Color.Lerp(bottom, top, t);
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateVerticalGradient(Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(1, 128);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[128];
            for (int i = 0; i < 128; i++)
            {
                float t = i / 127f;
                colors[i] = Color.Lerp(bottom, top, t);
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 128), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRadialGlowSprite(Color topColor, Color bottomColor, int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            var colors = new Color[size * size];
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = center.x;

            float solidRadius = Mathf.Max(0f, maxRadius - 3f);
            float fadeRadius = maxRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(distance / maxRadius);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    var color = Color.Lerp(topColor, bottomColor, eased);

                    float alphaFalloff = Mathf.InverseLerp(solidRadius, fadeRadius, distance);
                    color.a = 1f - Mathf.Clamp01(alphaFalloff);

                    colors[y * size + x] = color;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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

        /// <summary>Creates a glass-style content container that fills the screen above navigation.</summary>
        private (GameObject surface, RectTransform content) BuildPrototypeSurface(string name)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(_canvas.transform, false);

            var srt = surface.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(16f, 16f);
            srt.offsetMax = new Vector2(-16f, -16f);

            // Scrollable content container
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(surface.transform, false);

            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scroll.viewport = vpRT;

            // Content
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            scroll.content = crt;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            int horizontalPadding = 32;
            int topPadding = 40;
            int bottomPadding = Mathf.CeilToInt(40f + _navBarReservedHeight);
            vlg.padding = new RectOffset(horizontalPadding, horizontalPadding, topPadding, bottomPadding);
            vlg.spacing = 24f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentLayoutElement = content.AddComponent<LayoutElement>();
            contentLayoutElement.flexibleHeight = 1f;

            var prototypeContentFitter = content.AddComponent<PrototypeSurfaceContentFitter>();
            prototypeContentFitter.Initialize(vpRT, contentLayoutElement);

            return (surface, crt);
        }

        // ========= Navigation =========
        private const float NavBarHeight = 104f;

        private void CreateNavigationBar()
        {
            if (_navBar != null) Destroy(_navBar);

            _navBar = new GameObject("NavigationBar", typeof(RectTransform));
            _navBar.transform.SetParent(_canvas.transform, false);
            var rt = _navBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;

            float bottomInset = Screen.safeArea.y;
            _navBarReservedHeight = NavBarHeight + bottomInset;
            rt.sizeDelta = new Vector2(0f, _navBarReservedHeight);

            // Glass background effect
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(_navBar.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = _navBarGradient != null ? _navBarGradient : _fallbackGradient;
            bgImg.type = Image.Type.Simple;
            bgImg.color = Color.white;

            var topLine = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
            topLine.transform.SetParent(bgGO.transform, false);
            var topLineRT = topLine.GetComponent<RectTransform>();
            topLineRT.anchorMin = new Vector2(0f, 1f);
            topLineRT.anchorMax = new Vector2(1f, 1f);
            topLineRT.pivot = new Vector2(0.5f, 1f);
            topLineRT.sizeDelta = new Vector2(0f, 1.5f);
            var topLineImg = topLine.GetComponent<Image>();
            topLineImg.color = new Color(1f, 1f, 1f, 0.18f);

            // Navigation icons row
            var row = new GameObject("IconsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(bgGO.transform, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = Vector2.zero;
            rowRT.anchorMax = Vector2.one;
            rowRT.offsetMin = new Vector2(32f, 24f + bottomInset);
            rowRT.offsetMax = new Vector2(-32f, -12f);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 24f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandHeight = false;

            // Create navigation tabs matching prototype
            CreatePrototypeNavTab(row.transform, "Início", 0, _iconHome);
            CreatePrototypeNavTab(row.transform, "Categorias", 1, _iconCategories);
            CreatePrototypeNavTab(row.transform, "Timeline", 2, _iconTimeline);
            CreatePrototypeNavTab(row.transform, "QR Code", 3, _iconScanner);
            CreatePrototypeNavTab(row.transform, "Quiz", 4, _iconQuiz);

            _navBar.transform.SetAsLastSibling();
            RefreshTabsVisual();
        }

        private void CreatePrototypeNavTab(Transform parent, string tabName, int pageIndex, Sprite icon)
        {
            // Root tab
            var tab = new GameObject($"Tab_{tabName}", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            tab.transform.SetParent(parent, false);

            var le = tab.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = NavBarHeight;

            var bg = tab.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);

            var btn = tab.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                _activeTab = pageIndex;
                SwitchPage(pageIndex);
                RefreshTabsVisual();
            });

            // --- Content wrapper (layout-controlled) ---
            var content = new GameObject("Content", typeof(RectTransform), typeof(LayoutElement));
            content.transform.SetParent(tab.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.pivot = new Vector2(0.5f, 0.5f);
            contentRT.anchoredPosition = new Vector2(0f, 6f);              // lift a bit
            contentRT.sizeDelta = new Vector2(100f, 80f);                  // space for pill + icon

            var contentLE = content.GetComponent<LayoutElement>();
            contentLE.preferredHeight = 80f;

            // --- Overlapped icon group (pill behind icon) ---
            var iconGroup = new GameObject("IconGroup", typeof(RectTransform));
            iconGroup.transform.SetParent(content.transform, false);
            var igRT = iconGroup.GetComponent<RectTransform>();
            igRT.anchorMin = new Vector2(0.5f, 0.5f);
            igRT.anchorMax = new Vector2(0.5f, 0.5f);
            igRT.pivot = new Vector2(0.5f, 0.5f);
            igRT.anchoredPosition = Vector2.zero;
            igRT.sizeDelta = new Vector2(IconFrameSizeActive + 14f, IconFrameSizeActive + 14f);

            // Active pill (overlapped background)
            var activePill = new GameObject("ActivePill", typeof(RectTransform), typeof(Image));
            activePill.transform.SetParent(iconGroup.transform, false);
            var pillRT = activePill.GetComponent<RectTransform>();
            pillRT.anchorMin = new Vector2(0.5f, 0.5f);
            pillRT.anchorMax = new Vector2(0.5f, 0.5f);
            pillRT.pivot = new Vector2(0.5f, 0.5f);
            pillRT.anchoredPosition = Vector2.zero;
            pillRT.sizeDelta = new Vector2(IconFrameSizeActive + 14f, IconFrameSizeActive + 14f);

            var pillImg = activePill.GetComponent<Image>();
            var rounded = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (rounded != null) { pillImg.sprite = rounded; pillImg.type = Image.Type.Sliced; }
            pillImg.color = new Color(1f, 1f, 1f, 0f);  // hidden while inactive

            // Icon frame (glow)
            var iconFrameGO = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
            iconFrameGO.transform.SetParent(iconGroup.transform, false);
            var iconFrameRT = iconFrameGO.GetComponent<RectTransform>();
            iconFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconFrameRT.pivot = new Vector2(0.5f, 0.5f);
            iconFrameRT.anchoredPosition = Vector2.zero;
            iconFrameRT.sizeDelta = new Vector2(IconFrameSizeInactive, IconFrameSizeInactive);

            var iconFrameImg = iconFrameGO.GetComponent<Image>();
            iconFrameImg.sprite = _navIconGlow != null ? _navIconGlow : _fallbackGradient;
            iconFrameImg.type = Image.Type.Simple;
            iconFrameImg.preserveAspect = true;
            iconFrameImg.raycastTarget = false;
            iconFrameImg.color = IconBackgroundInactiveColor;

            // Icon glyph
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Outline));
            iconGO.transform.SetParent(iconFrameGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;
            iconRT.sizeDelta = new Vector2(IconGlyphSizeInactive, IconGlyphSizeInactive);

            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = icon != null ? icon : Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
            iconImg.color = IconInactiveColor;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var outline = iconGO.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.25f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;

            // Indicator (thin) — positioned under content, not in layout
            var indicatorGO = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            indicatorGO.transform.SetParent(tab.transform, false);
            var indicatorRT = indicatorGO.GetComponent<RectTransform>();
            indicatorRT.anchorMin = new Vector2(0.5f, 0f);
            indicatorRT.anchorMax = new Vector2(0.5f, 0f);
            indicatorRT.pivot = new Vector2(0.5f, 0f);
            indicatorRT.anchoredPosition = new Vector2(0f, 2f);
            indicatorRT.sizeDelta = new Vector2(56f, 3f);
            var indicatorImage = indicatorGO.GetComponent<Image>();
            indicatorImage.color = new Color(1f, 1f, 1f, 0f);

            // Label (fades in when active)
            var label = UiKit.TMP(tab.transform, tabName, 14, Color.white, TextAlignmentOptions.Midline, bold: false);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var labelCG = label.gameObject.AddComponent<CanvasGroup>();
            labelCG.alpha = 0f;

            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 6f);
            lrt.sizeDelta = new Vector2(100f, 18f);
        }

        private void RefreshTabsVisual()
        {
            if (_navBar == null) return;
            var row = _navBar.transform.Find("Background/IconsRow");
            if (!row) return;

            for (int i = 0; i < row.childCount; i++)
            {
                var tab = row.GetChild(i);
                bool active = (i == _activeTab);

                var pill = tab.Find("Content/IconGroup/ActivePill")?.GetComponent<Image>();
                var iconFrame = tab.Find("Content/IconGroup/IconFrame")?.GetComponent<Image>();
                var icon = tab.Find("Content/IconGroup/IconFrame/Icon")?.GetComponent<Image>();
                var indicator = tab.Find("Indicator")?.GetComponent<Image>();
                var label = tab.GetComponentsInChildren<TextMeshProUGUI>(true).LastOrDefault();
                var labelCG = label ? label.GetComponent<CanvasGroup>() : null;

                if (pill != null)
                    pill.color = active ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0f);

                if (iconFrame != null)
                {
                    iconFrame.color = active ? IconBackgroundActiveColor : IconBackgroundInactiveColor;
                    iconFrame.rectTransform.sizeDelta = active
                        ? new Vector2(IconFrameSizeActive, IconFrameSizeActive)
                        : new Vector2(IconFrameSizeInactive, IconFrameSizeInactive);
                    iconFrame.rectTransform.localScale = active ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                }

                if (icon != null)
                {
                    icon.color = active ? IconActiveColor : IconInactiveColor;
                    icon.rectTransform.sizeDelta = active
                        ? new Vector2(IconGlyphSizeActive, IconGlyphSizeActive)
                        : new Vector2(IconGlyphSizeInactive, IconGlyphSizeInactive);
                    icon.rectTransform.localScale = active ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
                }

                if (indicator != null)
                    indicator.color = active ? NavIndicatorColor : new Color(1f, 1f, 1f, 0f);

                if (labelCG != null) labelCG.alpha = active ? 0.95f : 0f;
            }
        }

        // ========= Pages =========

        private GameObject CreateHomePage()
        {
            // Criar o GameObject que irá conter a HomePage
            var homePageContainer = new GameObject("HomePageContainer");

            // Adicionar o componente HomePage
            _homePage = homePageContainer.AddComponent<HomePage>();

            // Configurar o evento de clique do botão "Buscar Peças"
            _homePage.OnSearchPiecesClicked += () =>
            {
                // Navegar para a página de categorias
                _activeTab = 1;
                SwitchPage(_activeTab);
                RefreshTabsVisual();
            };

            // Criar a página usando a nova classe
            var pageObject = _homePage.CreatePage(_canvas.transform, BuildPrototypeSurface);

            return pageObject;
        }

        /// <summary>
        /// Método auxiliar para acessar a página inicial
        /// </summary>
        public HomePage GetHomePage()
        {
            return _homePage;
        }

        /// <summary>
        /// Atualiza configurações da página inicial (exemplo de uso)
        /// </summary>
        public void UpdateHomePageSettings(float titleSize, float welcomeSize, float descSize)
        {
            if (_homePage != null)
            {
                _homePage.UpdateFontSizes(titleSize, welcomeSize, descSize);
            }
        }


        

        /// <summary>
        /// Cria a página de categorias usando a classe CategoriesPage separada
        /// </summary>
        private GameObject CreateCategoriesPage()
        {
            var categoriesPageContainer = new GameObject("CategoriesPageContainer");

            // Adicionar o componente CategoriesPage
            _categoriesPage = categoriesPageContainer.AddComponent<CategoriesPage>();

            // Configurar o evento de seleção de peça
            _categoriesPage.OnPieceSelected += (piece) =>
            {
                // Mostrar detalhes da peça selecionada
                ShowPieceDetail(piece);
            };

            // Criar a página usando a nova classe
            var pageObject = _categoriesPage.CreatePage(_canvas.transform, _canvas, BuildPrototypeSurface);

            return pageObject;
        }

        /// <summary>
        /// Método auxiliar para acessar a página de categorias
        /// </summary>
        public CategoriesPage GetCategoriesPage()
        {
            return _categoriesPage;
        }

        /// <summary>
        /// Atualiza configurações da página de categorias (exemplo de uso)
        /// </summary>
        public void UpdateCategoriesPageSettings(float headerHeight, float itemHeight, int titleSize, int itemSize)
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.UpdateVisualSettings(headerHeight, itemHeight, titleSize, itemSize);
            }
        }

        /// <summary>
        /// Expande ou recolhe uma categoria específica programaticamente
        /// </summary>
        /// <param name="categoryId">ID da categoria</param>
        /// <param name="expand">True para expandir, false para recolher</param>
        public void SetCategoryExpanded(long categoryId, bool expand)
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.SetCategoryExpanded(categoryId, expand);
            }
        }

        /// <summary>
        /// Fecha qualquer modal aberto na página de categorias
        /// </summary>
        public void CloseCategoriesModal()
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.CloseModal();
            }
        }



        private GameObject CreateScannerPage()
        {
            // Criar o GameObject que irá conter a ScannerPage
            var scannerPageContainer = new GameObject("ScannerPageContainer");

            // Adicionar o componente ScannerPage
            _scannerPage = scannerPageContainer.AddComponent<ScannerPage>();
            _scannerPage.SetQrCodeIcon(_iconScanner);

            // Configurar o evento de peça escaneada
            _scannerPage.OnPieceScanned += (piece) =>
            {
                // Mostrar detalhes da peça escaneada
                ShowPieceDetail(piece);
            };

            // Criar a página usando a nova classe
            var pageObject = _scannerPage.CreatePage(_canvas.transform, _canvas, BuildPrototypeSurface);

            return pageObject;
        }

        /// <summary>
        /// Método auxiliar para acessar a página do scanner
        /// </summary>
        public ScannerPage GetScannerPage()
        {
            return _scannerPage;
        }

        /// <summary>
        /// Para o escaneamento se estiver ativo
        /// </summary>
        public void StopScanning()
        {
            if (_scannerPage != null)
            {
                _scannerPage.StopScan();
            }
        }

        /// <summary>
        /// Verifica se está escaneando atualmente
        /// </summary>
        /// <returns>True se estiver escaneando, false caso contrário</returns>
        public bool IsScanning()
        {
            if (_scannerPage != null)
            {
                return _scannerPage.IsScanning();
            }
            return false;
        }

        /// <summary>
        /// Atualiza configurações da página do scanner (exemplo de uso)
        /// </summary>
        public void UpdateScannerPageSettings(float previewHeight, float tipsHeight, float frameSize)
        {
            if (_scannerPage != null)
            {
                _scannerPage.UpdateVisualSettings(previewHeight, tipsHeight, frameSize);
            }
        }

        /// <summary>
        /// Atualiza tamanhos das fontes da página do scanner
        /// </summary>
        public void UpdateScannerFontSizes(int titleSize, int scanTitleSize, int descSize, int tipsSize)
        {
            if (_scannerPage != null)
            {
                _scannerPage.UpdateFontSizes(titleSize, scanTitleSize, descSize, tipsSize);
            }
        }

       
        public void SwitchPage(int index)
        {
            if (_pages == null || index < 0 || index >= _pages.Length)
            {
                return;
            }

            if (_activeTab == 3 && index != 3)
            {
                StopScanning();
            }

            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null) _pages[i].SetActive(i == index);

            _activeTab = index;
            RefreshTabsVisual();
        }

       

        // ========= Helper Methods for Prototype UI =========

        private Image CreateGlassCard(Transform parent, float height)
        {
            var cardGO = new GameObject("GlassCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(parent, false);

            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0, 1);
            cardRT.anchorMax = new Vector2(1, 1);
            cardRT.pivot = new Vector2(0.5f, 1);
            cardRT.offsetMin = Vector2.zero;
            cardRT.offsetMax = Vector2.zero;

            var cardLE = cardGO.AddComponent<LayoutElement>();
            cardLE.preferredHeight = height;
            cardLE.minHeight = height;

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 0.1f); // Glass effect
            cardImg.raycastTarget = true;

            // Add rounded corners if you have a rounded sprite
            var roundedSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (roundedSprite != null)
            {
                cardImg.sprite = roundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            return cardImg;
        }

        private GameObject CreatePrototypeCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnCard = CreateGlassCard(parent, 48f);
            btnCard.color = Color.white; // White button like prototype

            var btn = btnCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnCard;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            var label = UiKit.TMP(btnCard.transform, text, 18, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 8);
            lrt.offsetMax = new Vector2(-16, -8);

            return btnCard.gameObject;
        }

        private GameObject CreateLargerCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnCard = CreateGlassCard(parent, 70f); // Taller button
            btnCard.color = Color.white;

            var btn = btnCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnCard;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            var label = UiKit.TMP(btnCard.transform, text, 28, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 12);
            lrt.offsetMax = new Vector2(-16, -12);

            return btnCard.gameObject;
        }


        private (GameObject header, RectTransform chevron) CreatePrototypeCategoryHeader(Transform parent, string title)
        {
            var headerCard = CreateGlassCard(parent, 56f);
            headerCard.color = new Color(1f, 1f, 1f, 0.15f); // Slightly more opaque for headers

            var btn = headerCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = headerCard;

            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.2f);
            colors.pressedColor = new Color(1, 1, 1, 0.25f);
            btn.colors = colors;

            var titleTMP = UiKit.TMP(headerCard.transform, title, 20, Color.white, TextAlignmentOptions.MidlineLeft, bold: false);
            titleTMP.enableWordWrapping = false;
            titleTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            titleTMP.rectTransform.anchorMin = Vector2.zero;
            titleTMP.rectTransform.anchorMax = Vector2.one;
            titleTMP.rectTransform.offsetMin = new Vector2(20, 8);
            titleTMP.rectTransform.offsetMax = new Vector2(-44, -8);
            titleTMP.raycastTarget = false;

            var chevron = CreateChevronIcon(headerCard.transform);
            return (headerCard.gameObject, chevron);
        }

        private RectTransform CreateChevronIcon(Transform parent)
        {
            var chevronGO = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            chevronGO.transform.SetParent(parent, false);
            var chevronRT = chevronGO.GetComponent<RectTransform>();
            chevronRT.anchorMin = new Vector2(1, 0.5f);
            chevronRT.anchorMax = new Vector2(1, 0.5f);
            chevronRT.pivot = new Vector2(1, 0.5f);
            chevronRT.sizeDelta = new Vector2(20, 20);
            chevronRT.anchoredPosition = new Vector2(-16, 0);

            var chevronImg = chevronGO.GetComponent<Image>();
            chevronImg.raycastTarget = false;

            // Try to load chevron sprite, fallback to text
            var chevronSprite = Resources.Load<Sprite>("Sprites/Chevron");
            if (chevronSprite != null)
            {
                chevronImg.sprite = chevronSprite;
                chevronImg.color = Color.white;
            }
            else
            {
                // Use text as fallback
                chevronGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                var textTMP = UiKit.TMP(chevronGO.transform, "▼", 16, Color.white, TextAlignmentOptions.Center);
                textTMP.raycastTarget = false;
                var textRT = textTMP.rectTransform;
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
            }

            return chevronRT;
        }

        private TimelinePage _timelinePage;

        // Substituir o método CreateTimelinePage() existente por este:
        private GameObject CreateTimelinePage()
        {
            // Criar o GameObject que irá conter a TimelinePage
            var timelinePageContainer = new GameObject("TimelinePageContainer");

            // Adicionar o componente TimelinePage
            _timelinePage = timelinePageContainer.AddComponent<TimelinePage>();

            // Configurar os eventos da timeline page
            _timelinePage.OnPieceSelected += (piece) =>
            {
                // Mostrar detalhes da peça selecionada
                ShowPieceDetail(piece);
            };

            _timelinePage.OnTimelineGenerated += (pieces) =>
            {
                // Opcional: log quando a timeline é gerada
                Debug.Log($"Timeline generated with {pieces.Count} pieces");
            };

            _timelinePage.OnSortOrderChanged += (sortOrder) =>
            {
                // Opcional: log quando a ordem é alterada
                Debug.Log($"Timeline sort order changed to: {sortOrder}");
            };

            // Criar a página usando a nova classe
            var pageObject = _timelinePage.CreatePage(
                _canvas.transform,
                BuildPrototypeSurface,
                CreateGlassCard,
                CreatePrototypeCTAButton
            );

            return pageObject;
        }

        // Métodos auxiliares para acessar a timeline page:

        /// <summary>
        /// Método auxiliar para acessar a página de timeline
        /// </summary>
        public TimelinePage GetTimelinePage()
        {
            return _timelinePage;
        }

        /// <summary>
        /// Atualiza configurações da página de timeline
        /// </summary>
        public void UpdateTimelinePageSettings(float cardHeight, float titleMargin, int titleSize, int yearSize,
                                              int nameSize, int descSize, int btnSize, float btnHeight)
        {
            if (_timelinePage != null)
            {
                _timelinePage.UpdateVisualSettings(cardHeight, titleMargin, titleSize, yearSize,
                                                 nameSize, descSize, btnSize, btnHeight);
            }
        }

        /// <summary>
        /// Muda a ordem de classificação da timeline
        /// </summary>
        public void ChangeTimelineSortOrder(TimelinePage.SortOrder sortOrder)
        {
            if (_timelinePage != null)
            {
                _timelinePage.ChangeSortOrder(sortOrder);
            }
        }

        /// <summary>
        /// Filtra a timeline por período
        /// </summary>
        public void FilterTimelineByYear(int startYear, int endYear)
        {
            if (_timelinePage != null)
            {
                _timelinePage.FilterByYearRange(startYear, endYear);
            }
        }

        /// <summary>
        /// Remove filtros da timeline
        /// </summary>
        public void ClearTimelineFilters()
        {
            if (_timelinePage != null)
            {
                _timelinePage.ClearFilters();
            }
        }

        /// <summary>
        /// Busca por peças na timeline
        /// </summary>
        public void SearchTimelinePieces(string searchTerm)
        {
            if (_timelinePage != null)
            {
                _timelinePage.SearchPieces(searchTerm);
            }
        }

        /// <summary>
        /// Obtém estatísticas da timeline
        /// </summary>
        public (int totalPieces, int earliestYear, int latestYear) GetTimelineStats()
        {
            if (_timelinePage != null)
            {
                return _timelinePage.GetTimelineStats();
            }
            return (0, 0, 0);
        }

        /// <summary>
        /// Obtém as peças atualmente exibidas na timeline
        /// </summary>
        public List<ComputerPiece> GetDisplayedTimelinePieces()
        {
            if (_timelinePage != null)
            {
                return _timelinePage.GetDisplayedPieces();
            }
            return new List<ComputerPiece>();
        }

        /// <summary>
        /// Exemplo de como usar os novos recursos da TimelinePage
        /// </summary>
        public void ExampleTimelineUsage()
        {
            // Mudar para ordem decrescente (mais recentes primeiro)
            ChangeTimelineSortOrder(TimelinePage.SortOrder.Descending);

            // Filtrar por década de 1970
            FilterTimelineByYear(1970, 1979);

            // Buscar por "Intel"
            SearchTimelinePieces("Intel");

            // Obter estatísticas
            var (total, earliest, latest) = GetTimelineStats();
            Debug.Log($"Timeline: {total} pieces from {earliest} to {latest}");

            // Limpar filtros
            ClearTimelineFilters();
        }


        // Substituir o método CreateQuizPage() existente por este:
        private GameObject CreateQuizPage()
        {
            // Criar o GameObject que irá conter a QuizPage
            var quizPageContainer = new GameObject("QuizPageContainer");

            // Adicionar o componente QuizPage
            _quizPage = quizPageContainer.AddComponent<QuizPage>();

            // Configurar os eventos da quiz page
            _quizPage.OnScoreUpdated += (currentScore, totalQuestions) =>
            {
                // Opcional: log ou outras ações quando a pontuação é atualizada
                Debug.Log($"Quiz Score Updated: {currentScore}/{totalQuestions}");
            };

            _quizPage.OnQuizCompleted += (finalScore) =>
            {
                // Opcional: ações quando o quiz é completado
                Debug.Log($"Quiz Completed! Final Score: {finalScore}");
            };

            _quizPage.OnQuestionAnswered += (question, wasCorrect) =>
            {
                // Opcional: ações quando uma pergunta é respondida
                Debug.Log($"Question answered: {question.Question} - {(wasCorrect ? "Correct" : "Incorrect")}");
            };


            
            _quizPage.OnReviewRequested += () =>
            {
                Debug.Log("Review content requested - navigating to Timeline");
                _activeTab = 2; 
                SwitchPage(_activeTab);
                RefreshTabsVisual();
            };

            _quizPage.OnExploreMuseumRequested += () =>
            {
                Debug.Log("Explore museum requested - navigating to Categories");
                _activeTab = 1; 
                SwitchPage(_activeTab);
                RefreshTabsVisual();
            };

            // Criar a página usando a nova classe
            var pageObject = _quizPage.CreatePage(
                _canvas.transform,
                BuildPrototypeSurface,
                CreateGlassCard,
                CreatePrototypeCTAButton
            );

            return pageObject;
        }

        // Métodos auxiliares para acessar a quiz page:

        /// <summary>
        /// Método auxiliar para acessar a página de quiz
        /// </summary>
        public QuizPage GetQuizPage()
        {
            return _quizPage;
        }

        /// <summary>
        /// Atualiza configurações da página de quiz
        /// </summary>
        public void UpdateQuizPageSettings(float headerH, float questionH, float optionH, float explanationH,
                                         int titleSize, int scoreSize, int questionSize, int optionSize)
        {
            if (_quizPage != null)
            {
                _quizPage.UpdateVisualSettings(headerH, questionH, optionH, explanationH,
                                             titleSize, scoreSize, questionSize, optionSize);
            }
        }

        /// <summary>
        /// Reinicia o quiz programaticamente
        /// </summary>
        public void RestartQuiz()
        {
            if (_quizPage != null)
            {
                _quizPage.RestartQuiz();
            }
        }

        /// <summary>
        /// Obtém a pontuação atual do quiz
        /// </summary>
        public int GetCurrentQuizScore()
        {
            return _quizPage != null ? _quizPage.GetCurrentScore() : 0;
        }

        /// <summary>
        /// Obtém a pontuação máxima do quiz
        /// </summary>
        public int GetQuizHighScore()
        {
            return _quizPage != null ? _quizPage.GetHighScore() : 0;
        }

        /// <summary>
        /// Versão atualizada do CloseModal que fecha modais de todas as páginas
        /// </summary>
        private void CloseModal()
        {
            // Fechar modal da página de categorias
            if (_categoriesPage != null)
                _categoriesPage.CloseModal();

            // Fechar outros modais se houver
            if (_openModal != null)
            {
                Destroy(_openModal);
                _openModal = null;
            }
        }
        private Transform CreateModalScrollView(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(parent, false);

            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = offsetMin;
            sRT.offsetMax = offsetMax;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.05f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vRT = viewport.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero;
            vRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scrollRect.viewport = vRT;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;
            scrollRect.content = cRT;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return content.transform;
        }


        private void ShowPieceDetail(ComputerPiece piece)
        {
            CloseModal();

            var overlay = new GameObject("PieceDetailOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(_canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            _openModal = overlay;

            var ovRT = overlay.GetComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero;
            ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = Vector2.zero;
            ovRT.offsetMax = Vector2.zero;

            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            overlay.GetComponent<Button>().onClick.AddListener(CloseModal);

            // Full screen detail view with gradient background
            var detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detailPanel.transform.SetParent(overlay.transform, false);

            var dRT = detailPanel.GetComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero;
            dRT.anchorMax = Vector2.one;
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;

            var dImg = detailPanel.GetComponent<Image>();
            dImg.sprite = _fallbackGradient;
            dImg.color = Color.white;

            // Header with back button
            var headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            headerGO.transform.SetParent(detailPanel.transform, false);
            var headerRT = headerGO.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.offsetMin = new Vector2(16, -80);
            headerRT.offsetMax = new Vector2(-16, -16);

            var headerHLG = headerGO.GetComponent<HorizontalLayoutGroup>();
            headerHLG.childAlignment = TextAnchor.MiddleLeft;
            headerHLG.spacing = 12;

            // Back button
            var backBtn = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            backBtn.transform.SetParent(headerGO.transform, false);
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.sizeDelta = new Vector2(40, 40);

            var backImage = backBtn.GetComponent<Image>();
            backImage.color = new Color(1, 1, 1, 0.2f);
            backBtn.GetComponent<Button>().onClick.AddListener(CloseModal);

            var backIconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            backIconGO.transform.SetParent(backBtn.transform, false);
            var backIconRT = backIconGO.GetComponent<RectTransform>();
            backIconRT.anchorMin = new Vector2(0.2f, 0.2f);
            backIconRT.anchorMax = new Vector2(0.8f, 0.8f);
            backIconRT.offsetMin = Vector2.zero;
            backIconRT.offsetMax = Vector2.zero;

            var backIconImage = backIconGO.GetComponent<Image>();
            backIconImage.sprite = IconFactory.GetBackIcon();
            backIconImage.color = Color.white;
            backIconImage.preserveAspect = true;
            backIconImage.raycastTarget = false;

            // App title
            UiKit.TMP(headerGO.transform, "RetroTech", 28, Color.white, TextAlignmentOptions.Left, bold: true);

            // Content scroll view
            var contentScroll = CreateModalScrollView(detailPanel.transform, new Vector2(16, 16), new Vector2(-16, -96));

            // Piece details
            AddDetailSection(contentScroll, piece);
        }

        private void AddDetailSection(Transform parent, ComputerPiece piece)
        {
            // Hero image
            const float heroImageHeight = 260f;
            var heroCard = CreateGlassCard(parent, heroImageHeight);
            var heroLayout = heroCard.GetComponent<LayoutElement>() ?? heroCard.gameObject.AddComponent<LayoutElement>();
            heroLayout.preferredHeight = heroImageHeight;
            heroLayout.minHeight = heroImageHeight;
            heroLayout.flexibleHeight = 0f;
            heroCard.raycastTarget = false;

            if (!heroCard.gameObject.TryGetComponent<RectMask2D>(out _))
            {
                heroCard.gameObject.AddComponent<RectMask2D>();
            }

            var heroSprite = PieceImageFactory.GetSprite(piece);
            if (heroSprite != null)
            {
                var imageGO = new GameObject("HeroImage", typeof(RectTransform), typeof(Image));
                imageGO.transform.SetParent(heroCard.transform, false);

                var imageRT = imageGO.GetComponent<RectTransform>();
                imageRT.anchorMin = Vector2.zero;
                imageRT.anchorMax = Vector2.one;
                imageRT.offsetMin = new Vector2(16f, 16f);
                imageRT.offsetMax = new Vector2(-16f, -16f);

                var heroImage = imageGO.GetComponent<Image>();
                heroImage.sprite = heroSprite;
                heroImage.color = Color.white;
                heroImage.preserveAspect = true;
                heroImage.raycastTarget = false;

                var aspect = imageGO.GetComponent<AspectRatioFitter>() ?? imageGO.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                if (heroSprite.rect.height > 0f)
                {
                    aspect.aspectRatio = heroSprite.rect.width / heroSprite.rect.height;
                }
            }
            else
            {
                heroCard.color = new Color(1f, 1f, 1f, 0.15f);
            }

            // Title
            UiKit.TMP(parent, piece.Name, 50, Color.white, TextAlignmentOptions.Left, bold: true);

            // Year
            AddDetailField(parent, "Ano de fabricação", piece.YearManufactured > 0 ? piece.YearManufactured.ToString() : "-");

            // Manufacturer
            AddDetailField(parent, "Fabricante", !string.IsNullOrEmpty(piece.Manufacturer) ? piece.Manufacturer : "-");

            // Description
            AddDetailSection(parent, "Descrição", !string.IsNullOrEmpty(piece.Description) ? piece.Description : "Sem descrição.");

            // Curiosities
            AddDetailSection(parent, "Curiosidades", !string.IsNullOrEmpty(piece.Curiosities) ? piece.Curiosities : "—");


            AddSpacer(parent, 40);
        }


        private void AddDetailField(Transform parent, string label, string value)
        {
            UiKit.TMP(parent, label, 46, new Color32(255, 255, 255, 150), TextAlignmentOptions.Left,bold:true);
            var valueTMP = UiKit.TMP(parent, value, 36, Color.white, TextAlignmentOptions.Left, bold: true);
            valueTMP.margin = new Vector4(0, 0, 0, 16);
        }

        private void AddDetailSection(Transform parent, string title, string content)
        {
            UiKit.TMP(parent, title, 46, Color.white, TextAlignmentOptions.Left, bold: true);
            var contentTMP = UiKit.TMP(parent, content, 36, new Color32(255, 255, 255, 200), TextAlignmentOptions.Left);
            contentTMP.enableWordWrapping = true;
            contentTMP.margin = new Vector4(0, 0, 0, 20);
        }

        // ========= Search Feature =========

        private void CreateOrShowSearchPage()
        {
            if (_pages.Length < 6)
            {
                var newPages = new List<GameObject>(_pages);
                var searchPage = CreateSearchPage();
                newPages.Add(searchPage);
                _pages = newPages.ToArray();
                AddSearchNavTab();
            }
            SwitchPage(_pages.Length - 1);
        }

        private void AddSearchNavTab()
        {
            var row = _navBar.transform.Find("Background/IconsRow");
            CreatePrototypeNavTab(row, "Pesquisa", _pages.Length - 1, _iconHome);
            RefreshTabsVisual();
        }

        private GameObject CreateSearchPage()
        {
            var (surface, content) = BuildPrototypeSurface("SearchPage");

            // Search title
            UiKit.TMP(content, "Pesquisar Peças", 36, Color.white, TextAlignmentOptions.Left, bold: true);

            // Search input
            var searchCard = CreateGlassCard(content, 48f);
            searchCard.color = new Color(1f, 1f, 1f, 0.15f);

            var inputField = searchCard.gameObject.AddComponent<InputField>();

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(searchCard.transform, false);
            var placeholder = placeholderGO.AddComponent<Text>();
            placeholder.text = "Buscar por nome...";
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = ResponsiveTypography.ResponsiveFontSize(18);
            placeholder.alignment = TextAnchor.MiddleLeft;

            var phRT = placeholderGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(16, 0);
            phRT.offsetMax = new Vector2(-16, 0);

            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(searchCard.transform, false);
            var text = textGO.AddComponent<Text>();
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = ResponsiveTypography.ResponsiveFontSize(18);
            text.alignment = TextAnchor.MiddleLeft;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(16, 0);
            textRT.offsetMax = new Vector2(-16, 0);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            // Sort dropdown
            var sortCard = CreateGlassCard(content, 48f);
            sortCard.color = new Color(1f, 1f, 1f, 0.15f);

            var dropdown = sortCard.gameObject.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData> {
                new Dropdown.OptionData("Alfabético"),
                new Dropdown.OptionData("Ano crescente"),
                new Dropdown.OptionData("Ano decrescente")
            };

            // Search button
            var searchBtn = CreatePrototypeCTAButton(content.transform, "Buscar", () =>
            {
                PerformSearch(inputField.text, dropdown.value, content);
            });

            // Results area
            var resultsGO = new GameObject("SearchResults", typeof(RectTransform), typeof(LayoutElement));
            resultsGO.transform.SetParent(content, false);
            var resultsLE = resultsGO.GetComponent<LayoutElement>();
            resultsLE.flexibleHeight = 1;
            resultsLE.minHeight = 200;

            return surface;
        }

        private void PerformSearch(string query, int sortType, Transform contentParent)
        {
            // Find existing results area or create new one
            var resultsGO = contentParent.Find("SearchResults");
            if (resultsGO == null) return;

            // Clear previous results
            foreach (Transform child in resultsGO)
                Destroy(child.gameObject);

            // Filter pieces
            var filtered = new List<ComputerPiece>();
            foreach (var piece in SampleData.Pieces)
            {
                if (string.IsNullOrEmpty(query) || piece.Name.ToLower().Contains(query.ToLower()))
                    filtered.Add(piece);
            }

            // Sort pieces
            switch (sortType)
            {
                case 0: // Alphabetical
                    filtered.Sort((a, b) => a.Name.CompareTo(b.Name));
                    break;
                case 1: // Year ascending
                    filtered.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured));
                    break;
                case 2: // Year descending
                    filtered.Sort((a, b) => b.YearManufactured.CompareTo(a.YearManufactured));
                    break;
            }

            // Add vertical layout to results
            var vlg = resultsGO.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = resultsGO.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
            }

            // Display results
            foreach (var piece in filtered)
            {
                var resultCard = CreateGlassCard(resultsGO, 56f);
                resultCard.color = new Color(1f, 1f, 1f, 0.1f);

                var btn = resultCard.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => ShowPieceDetail(piece));

                var resultTMP = UiKit.TMP(resultCard.transform,
                    $"{piece.Name} ({piece.YearManufactured}) - {piece.Description}",
                    16, Color.white, TextAlignmentOptions.MidlineLeft);
                resultTMP.enableWordWrapping = false;
                resultTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                resultTMP.raycastTarget = false;

                var resultRT = resultTMP.rectTransform;
                resultRT.anchorMin = Vector2.zero;
                resultRT.anchorMax = Vector2.one;
                resultRT.offsetMin = new Vector2(16, 8);
                resultRT.offsetMax = new Vector2(-16, -8);
            }

            // Add results count
            if (filtered.Count == 0)
            {
                var noResultsTMP = UiKit.TMP(resultsGO, "Nenhuma peça encontrada.", 18, new Color32(255, 255, 255, 150), TextAlignmentOptions.Center);
            }
        }

        private void OnDestroy()
        {
            _apiClient?.Dispose();
        }

        // ========= Utility Methods =========

        private void AddSpacer(Transform parent, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }
    }
}