using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RetroTech
{
    /// <summary>
    /// Builds the authentication surface used to capture login and registration data before
    /// the main RetroTech experience is available.  The page is created entirely in code so it
    /// matches the rest of the prototype driven UI.
    /// </summary>
    public class LoginPage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "Autenticação";

        [Header("Visual Configuration")]
        [SerializeField] private Color32 tabInactiveColor = new Color32(255, 255, 255, 32);
        [SerializeField] private Color32 tabActiveColor = new Color32(114, 74, 160, 255);
        [SerializeField] private Color32 buttonColor = new Color32(114, 74, 160, 255);
        [SerializeField] private Color32 buttonTextColor = new Color32(250, 250, 255, 255);
        [SerializeField] private Color32 errorColor = new Color32(255, 105, 97, 255);
        [SerializeField] private Color32 successColor = new Color32(144, 238, 144, 255);
        [SerializeField] private int pageTitleFontSize = UiKit.DefaultPageTitleFontSize;

        public event Action<string, string> OnLoginRequested;
        public event Action<string, string> OnRegisterRequested;

        private GameObject _pageObject;
        private RectTransform _contentContainer;

        private GameObject _loginForm;
        private GameObject _registerForm;

        private Button _loginSubmitButton;
        private Button _registerSubmitButton;

        private TMP_InputField _loginUsernameInput;
        private TMP_InputField _loginPasswordInput;
        private TMP_InputField _registerUsernameInput;
        private TMP_InputField _registerPasswordInput;

        private Button _loginTabButton;
        private Image _loginTabBackground;
        private TextMeshProUGUI _loginTabLabel;

        private Button _registerTabButton;
        private Image _registerTabBackground;
        private TextMeshProUGUI _registerTabLabel;

        private TextMeshProUGUI _messageLabel;

        private bool _showingRegister;

        /// <summary>
        /// Creates the login page surface using the shared prototype builder.
        /// </summary>
        public GameObject CreatePage(Transform parent, Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc)
        {
            var (surface, content) = buildSurfaceFunc.Invoke(pageTitle);
            _pageObject = surface;
            _contentContainer = content;
            _pageObject.name = "LoginPage";

            BuildLayout();

            Hide();
            return _pageObject;
        }

        /// <summary>
        /// Displays the login/registration surface.
        /// </summary>
        public void ShowLogin()
        {
            if (_pageObject == null)
                return;

            _pageObject.SetActive(true);
            _pageObject.transform.SetAsLastSibling();
            SwitchToLogin();
        }

        /// <summary>
        /// Displays the registration surface directly.
        /// </summary>
        public void ShowRegister()
        {
            if (_pageObject == null)
                return;

            _pageObject.SetActive(true);
            _pageObject.transform.SetAsLastSibling();
            SwitchToRegister();
        }

        /// <summary>
        /// Hides the surface completely.
        /// </summary>
        public void Hide()
        {
            if (_pageObject != null)
            {
                _pageObject.SetActive(false);
            }
        }

        /// <summary>
        /// Enables or disables the submission buttons to prevent duplicate requests.
        /// </summary>
        public void SetBusy(bool busy)
        {
            if (_loginSubmitButton != null)
                _loginSubmitButton.interactable = !busy;

            if (_registerSubmitButton != null)
                _registerSubmitButton.interactable = !busy;

            if (_loginTabButton != null)
                _loginTabButton.interactable = !busy || !_showingRegister;

            if (_registerTabButton != null)
                _registerTabButton.interactable = !busy || _showingRegister;
        }

        /// <summary>
        /// Updates the feedback label with contextual information.
        /// </summary>
        public void ShowMessage(string message, bool isError)
        {
            if (_messageLabel == null)
                return;

            _messageLabel.text = message ?? string.Empty;
            _messageLabel.color = isError ? errorColor : successColor;
            _messageLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        /// <summary>
        /// Clears passwords from both forms to avoid keeping sensitive information in memory.
        /// </summary>
        public void ClearPasswords()
        {
            if (_loginPasswordInput != null)
                _loginPasswordInput.text = string.Empty;

            if (_registerPasswordInput != null)
                _registerPasswordInput.text = string.Empty;
        }

        /// <summary>
        /// Resets the page to its default state.
        /// </summary>
        public void ResetState()
        {
            ShowMessage(string.Empty, false);
            ClearPasswords();
            SwitchToLogin();
            SetBusy(false);
        }

        private void BuildLayout()
        {
            CreateHeader();
            CreateTabs();
            CreateForms();
            CreateMessageLabel();
        }

        private void CreateHeader()
        {
            var (titleContainer, title) = UiKit.CreatePageTitle(_contentContainer, "Bem-vindo ao RetroTech",
                pageTitleFontSize, UiKit.TextMain, TextAlignmentOptions.Left);
            titleContainer.name = "LoginPageTitle";
            if (title != null)
            {
                title.raycastTarget = false;
            }

            var subtitle = UiKit.TMP(_contentContainer, "Faça login ou crie sua conta para acessar o acervo.", 32,
                UiKit.TextMuted, TextAlignmentOptions.Left);
            subtitle.margin = new Vector4(0, 12, 0, 30);
        }

        private void CreateTabs()
        {
            var tabsRow = new GameObject("TabsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsRow.transform.SetParent(_contentContainer, false);

            var rowRT = tabsRow.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.offsetMin = Vector2.zero;
            rowRT.offsetMax = Vector2.zero;

            var hlg = tabsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.UpperCenter;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = false;

            (_loginTabButton, _loginTabBackground, _loginTabLabel) = CreateTabButton(tabsRow.transform, "Entrar");
            (_registerTabButton, _registerTabBackground, _registerTabLabel) = CreateTabButton(tabsRow.transform, "Cadastrar");

            _loginTabButton.onClick.AddListener(SwitchToLogin);
            _registerTabButton.onClick.AddListener(SwitchToRegister);
        }

        private (Button button, Image background, TextMeshProUGUI label) CreateTabButton(Transform parent, string label)
        {
            var go = new GameObject($"{label}Tab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = 64f;
            layout.flexibleWidth = 1f;

            var background = go.GetComponent<Image>();
            background.color = tabInactiveColor;
            background.raycastTarget = true;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = new Color(background.color.r, background.color.g, background.color.b, 0.5f);
            colors.pressedColor = new Color(background.color.r, background.color.g, background.color.b, 0.8f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var labelTMP = UiKit.TMP(go.transform, label, 28, UiKit.TextMain, TextAlignmentOptions.Center, bold: true);
            labelTMP.raycastTarget = false;
            var lrt = labelTMP.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 12);
            lrt.offsetMax = new Vector2(-16, -12);

            return (button, background, (TextMeshProUGUI)labelTMP);
        }

        private void CreateForms()
        {
            _loginForm = CreateFormContainer("LoginForm");
            _registerForm = CreateFormContainer("RegisterForm");

            BuildLoginForm();
            BuildRegisterForm();

            _registerForm.SetActive(false);
        }

        private GameObject CreateFormContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(_contentContainer, false);

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        private void BuildLoginForm()
        {
            UiKit.TMP(_loginForm.transform, "Usuário", 28, UiKit.TextMain, TextAlignmentOptions.Left, bold: true);
            _loginUsernameInput = CreateInputField(_loginForm.transform, "Digite seu nome de usuário", TMP_InputField.ContentType.Standard, false);

            UiKit.TMP(_loginForm.transform, "Senha", 28, UiKit.TextMain, TextAlignmentOptions.Left, bold: true);
            _loginPasswordInput = CreateInputField(_loginForm.transform, "Digite sua senha", TMP_InputField.ContentType.Password, true);

            _loginSubmitButton = CreatePrimaryButton(_loginForm.transform, "Entrar");
            _loginSubmitButton.onClick.AddListener(HandleLoginClicked);
        }

        private void BuildRegisterForm()
        {
            UiKit.TMP(_registerForm.transform, "Usuário", 28, UiKit.TextMain, TextAlignmentOptions.Left, bold: true);
            _registerUsernameInput = CreateInputField(_registerForm.transform, "Escolha um nome de usuário", TMP_InputField.ContentType.Standard, false);

            UiKit.TMP(_registerForm.transform, "Senha", 28, UiKit.TextMain, TextAlignmentOptions.Left, bold: true);
            _registerPasswordInput = CreateInputField(_registerForm.transform, "Crie uma senha", TMP_InputField.ContentType.Password, true);

            _registerSubmitButton = CreatePrimaryButton(_registerForm.transform, "Criar conta");
            _registerSubmitButton.onClick.AddListener(HandleRegisterClicked);
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholder, TMP_InputField.ContentType contentType, bool isPassword)
        {
            var fieldGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            fieldGO.transform.SetParent(parent, false);

            var background = fieldGO.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);
            background.raycastTarget = true;

            var layout = fieldGO.GetComponent<LayoutElement>();
            float responsivePreferred = ResponsiveTypography.ResponsiveSpacing(70f);
            float responsiveMin = ResponsiveTypography.ResponsiveSpacing(60f);
            layout.preferredHeight = responsivePreferred;
            layout.minHeight = responsiveMin;

            var input = fieldGO.AddComponent<TMP_InputField>();
            input.contentType = contentType;
            input.inputType = isPassword ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.AutoCorrect;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.75f, 0.75f, 1f, 0.4f);

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGO.transform, false);
            var areaRT = textArea.GetComponent<RectTransform>();
            areaRT.anchorMin = Vector2.zero;
            areaRT.anchorMax = Vector2.one;
            areaRT.offsetMin = new Vector2(16, 12);
            areaRT.offsetMax = new Vector2(-16, -12);

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(textArea.transform, false);
            var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = placeholder;
            ResponsiveTypography.ApplyToTMP(placeholderTMP, 28);
            placeholderTMP.color = new Color(1f, 1f, 1f, 0.5f);
            placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textArea.transform, false);
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            ResponsiveTypography.ApplyToTMP(textTMP, 30, allowShrink: false);
            textTMP.color = UiKit.TextMain;
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderRT = placeholderTMP.rectTransform;
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;

            var textRT = textTMP.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            input.textViewport = areaRT;
            input.textComponent = textTMP;
            input.placeholder = placeholderTMP;

            if (isPassword)
            {
                input.inputType = TMP_InputField.InputType.Password;
                input.contentType = TMP_InputField.ContentType.Password;
            }

            return input;
        }

        private Button CreatePrimaryButton(Transform parent, string text)
        {
            var buttonGO = new GameObject($"{text.Replace(" ", string.Empty)}Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(parent, false);

            var layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredHeight = 68f;
            layout.minHeight = 60f;

            var background = buttonGO.GetComponent<Image>();
            background.color = buttonColor;
            background.raycastTarget = true;

            var button = buttonGO.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(buttonColor.r + 0.05f, buttonColor.g + 0.05f, buttonColor.b + 0.05f, buttonColor.a);
            colors.pressedColor = new Color(buttonColor.r * 0.9f, buttonColor.g * 0.9f, buttonColor.b * 0.9f, buttonColor.a);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var labelTMP = UiKit.TMP(buttonGO.transform, text, 30, buttonTextColor, TextAlignmentOptions.Center, bold: true);
            labelTMP.raycastTarget = false;
            var lrt = labelTMP.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 12);
            lrt.offsetMax = new Vector2(-16, -12);

            return button;
        }

        private void CreateMessageLabel()
        {
            _messageLabel =(TextMeshProUGUI) UiKit.TMP(_contentContainer, string.Empty, 26, successColor, TextAlignmentOptions.Left);
            _messageLabel.gameObject.SetActive(false);
            _messageLabel.margin = new Vector4(0, 20, 0, 0);
        }

        private void SwitchToLogin()
        {
            if (_loginForm == null || _registerForm == null)
                return;

            _showingRegister = false;
            _loginForm.SetActive(true);
            _registerForm.SetActive(false);
            UpdateTabVisuals();
        }

        private void SwitchToRegister()
        {
            if (_loginForm == null || _registerForm == null)
                return;

            _showingRegister = true;
            _loginForm.SetActive(false);
            _registerForm.SetActive(true);
            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            if (_loginTabBackground != null)
            {
                _loginTabBackground.color = _showingRegister ? tabInactiveColor : tabActiveColor;
                if (_loginTabLabel != null)
                    _loginTabLabel.color = _showingRegister ? UiKit.TextMain : buttonTextColor;
            }

            if (_registerTabBackground != null)
            {
                _registerTabBackground.color = _showingRegister ? tabActiveColor : tabInactiveColor;
                if (_registerTabLabel != null)
                    _registerTabLabel.color = _showingRegister ? buttonTextColor : UiKit.TextMain;
            }
        }

        private void HandleLoginClicked()
        {
            OnLoginRequested?.Invoke(_loginUsernameInput?.text ?? string.Empty, _loginPasswordInput?.text ?? string.Empty);
        }

        private void HandleRegisterClicked()
        {
            OnRegisterRequested?.Invoke(
                _registerUsernameInput?.text ?? string.Empty,
                _registerPasswordInput?.text ?? string.Empty);
        }
    }
}
