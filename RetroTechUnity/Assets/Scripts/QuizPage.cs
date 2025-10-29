using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página do quiz do RetroTech com UI aprimorada - inclui animações suaves,
    /// feedback visual rico, gradientes e uma experiência mais polida.
    /// </summary>
    public class QuizPage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "QuizPage";

        [Header("Visual Configuration")]
        [SerializeField] private float headerHeight = 100f;
        [SerializeField] private float questionCardHeight = 200f;
        [SerializeField] private float optionHeight = 72f;
        [SerializeField] private float explanationHeight = 280f; // Much larger
        [SerializeField] private int pageTitleFontSize = 44;
        [SerializeField] private int questionNumberFontSize = 24;
        [SerializeField] private int scoreFontSize = 28;
        [SerializeField] private int questionFontSize = 28;
        [SerializeField] private int optionFontSize = 24;
        [SerializeField] private int explanationTitleFontSize = 34; // Much larger (was 24)
        [SerializeField] private int explanationTextFontSize = 30; // Much larger (was 22)

        // Enhanced Colors with better contrast for readability
        private readonly Color HeaderGradientTop = new Color32(147, 112, 219, 255);
        private readonly Color HeaderGradientBottom = new Color32(114, 74, 160, 255);
        private readonly Color QuestionCardColor = new Color32(0, 0, 0, 60); // Much darker for better text contrast
        private readonly Color QuestionNumberColor = new Color32(200, 180, 255, 255); // Lighter purple for visibility
        private readonly Color OptionColor = new Color32(255, 255, 255, 255);
        private readonly Color OptionHoverColor = new Color32(147, 112, 219, 60);
        private readonly Color OptionTextColor = new Color32(40, 20, 80, 255); // Much darker for better readability
        private readonly Color ProgressBackgroundColor = new Color32(0, 0, 0, 40); // Darker for visibility
        private readonly Color ProgressFillColor = new Color32(147, 112, 219, 255);
        private readonly Color CorrectOptionColor = new Color32(76, 175, 80, 255);
        private readonly Color WrongOptionColor = new Color32(244, 67, 54, 255);
        private readonly Color DisabledOptionColor = new Color32(180, 180, 180, 255);
        private readonly Color ExplanationCorrectBg = new Color32(76, 175, 80, 70); // More opaque for readability
        private readonly Color ExplanationWrongBg = new Color32(244, 67, 54, 70); // More opaque for readability
        private readonly Color ShadowColor = new Color(0, 0, 0, 0.25f);

        // Events
        public System.Action<int, int> OnScoreUpdated;
        public System.Action<int> OnQuizCompleted;
        public System.Action<QuizQuestion, bool> OnQuestionAnswered;

        // State
        private int _currentQuizIndex;
        private int _quizScore;
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private bool _answerSelected;

        // UI References
        private TextMeshProUGUI _pageTitleLabel;
        private TextMeshProUGUI _questionNumberLabel;
        private TextMeshProUGUI _scoreLabel;
        private Image _progressFill;
        private GameObject _progressGlow;
        private GameObject _nextButton;
        private TextMeshProUGUI _explanationTitleText;
        private TextMeshProUGUI _explanationText;
        private TextMeshProUGUI _questionText;
        private GameObject _optionsContainer;
        private GameObject _explanationCard;
        private Image _explanationCardBackground;
        private List<GameObject> _optionButtons = new List<GameObject>();

        // Animation helpers
        private Coroutine _currentAnimation;

        /// <summary>
        /// Cria e configura a página do quiz
        /// </summary>
        public GameObject CreatePage(
            Transform parent,
            System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc,
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            CreateQuizContent(createGlassCardFunc, createCTAButtonFunc);
            InitializeQuiz();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo do quiz
        /// </summary>
        private void CreateQuizContent(
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            // Page title
            CreatePageTitle();

            AddSpacer(_contentContainer, 16f);

            // Enhanced header with gradient and stats
            CreateEnhancedHeader(createGlassCardFunc);

            AddSpacer(_contentContainer, 20f);

            // Enhanced progress bar with glow
            CreateEnhancedProgressBar(createGlassCardFunc);

            AddSpacer(_contentContainer, 24f);

            // Question card with better typography
            CreateEnhancedQuestionCard(createGlassCardFunc);

            AddSpacer(_contentContainer, 20f);

            // Options container
            CreateOptionsContainer();

            AddSpacer(_contentContainer, 20f);

            // Enhanced explanation card
            CreateEnhancedExplanationCard(createGlassCardFunc);

            AddSpacer(_contentContainer, 16f);

            // Next button
            CreateNextButton(createCTAButtonFunc);

            AddSpacer(_contentContainer, 40f);
        }

        /// <summary>
        /// Cria o título da página
        /// </summary>
        private void CreatePageTitle()
        {
            _pageTitleLabel = (TextMeshProUGUI)UiKit.TMP(_contentContainer, "Quiz RetroTech", pageTitleFontSize,
                Color.white, TextAlignmentOptions.Center, bold: true);
            _pageTitleLabel.fontStyle = FontStyles.Bold;
            _pageTitleLabel.margin = new Vector4(0, 0, 0, 0);

            // Add subtle text outline for better visibility (no shadow)
            _pageTitleLabel.outlineWidth = 0.2f;
            _pageTitleLabel.outlineColor = new Color32(0, 0, 0, 100);
        }

        /// <summary>
        /// Cria um header aprimorado com gradiente
        /// </summary>
        private void CreateEnhancedHeader(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var headerCard = createGlassCardFunc(_contentContainer, headerHeight);

            // Create gradient effect
            var gradientGO = new GameObject("HeaderGradient", typeof(RectTransform), typeof(Image));
            gradientGO.transform.SetParent(headerCard.transform, false);
            gradientGO.transform.SetAsFirstSibling();

            var gradRT = gradientGO.GetComponent<RectTransform>();
            gradRT.anchorMin = Vector2.zero;
            gradRT.anchorMax = Vector2.one;
            gradRT.offsetMin = Vector2.zero;
            gradRT.offsetMax = Vector2.zero;

            var gradImg = gradientGO.GetComponent<Image>();
            gradImg.color = new Color(1, 1, 1, 0.15f);
            gradImg.raycastTarget = false;

            var headerVLG = headerCard.gameObject.AddComponent<VerticalLayoutGroup>();
            headerVLG.padding = new RectOffset(24, 24, 16, 16);
            headerVLG.spacing = 8;
            headerVLG.childAlignment = TextAnchor.MiddleCenter;
            headerVLG.childControlWidth = true;
            headerVLG.childForceExpandWidth = true;
            headerVLG.childControlHeight = false;
            headerVLG.childForceExpandHeight = false;

            // Question number
            _questionNumberLabel = (TextMeshProUGUI)UiKit.TMP(headerCard.transform, "Pergunta 1 de 5", questionNumberFontSize,
                QuestionNumberColor, TextAlignmentOptions.Center, bold: true);
            _questionNumberLabel.enableWordWrapping = false;
            _questionNumberLabel.fontStyle = FontStyles.Bold; // Extra bold
            _questionNumberLabel.outlineWidth = 0.15f; // Slight outline for visibility
            _questionNumberLabel.outlineColor = new Color32(0, 0, 0, 80);

            // Score with icon
            var scoreContainer = new GameObject("ScoreContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            scoreContainer.transform.SetParent(headerCard.transform, false);

            var scoreHLG = scoreContainer.GetComponent<HorizontalLayoutGroup>();
            scoreHLG.spacing = 8;
            scoreHLG.childAlignment = TextAnchor.MiddleCenter;
            scoreHLG.childControlWidth = false;
            scoreHLG.childForceExpandWidth = false;

            // Trophy icon (using emoji)
            var trophyTMP = UiKit.TMP(scoreContainer.transform, "🏆", scoreFontSize,
                new Color32(255, 215, 0, 255), TextAlignmentOptions.Center);
            trophyTMP.enableWordWrapping = false;

            _scoreLabel = (TextMeshProUGUI)UiKit.TMP(scoreContainer.transform, "0 pontos", scoreFontSize,
                Color.white, TextAlignmentOptions.Center, bold: true);
            _scoreLabel.enableWordWrapping = false;
        }

        /// <summary>
        /// Cria uma barra de progresso aprimorada com brilho
        /// </summary>
        private void CreateEnhancedProgressBar(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var progressCard = createGlassCardFunc(_contentContainer, 16f);
            progressCard.color = ProgressBackgroundColor;

            // Add rounded corners simulation
            var progressRT = progressCard.GetComponent<RectTransform>();

            // Progress fill with smooth edges
            var fillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(progressCard.transform, false);

            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = new Vector2(3, 3);
            fillRT.offsetMax = new Vector2(3, -3);

            _progressFill = fillGO.GetComponent<Image>();
            _progressFill.color = ProgressFillColor;
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillAmount = 0f;

            // Add glow effect
            _progressGlow = new GameObject("ProgressGlow", typeof(RectTransform), typeof(Image));
            _progressGlow.transform.SetParent(progressCard.transform, false);

            var glowRT = _progressGlow.GetComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = new Vector2(0, 1);
            glowRT.offsetMin = new Vector2(3, 3);
            glowRT.offsetMax = new Vector2(3, -3);

            var glowImg = _progressGlow.GetComponent<Image>();
            glowImg.color = new Color32(147, 112, 219, 100);
            glowImg.type = Image.Type.Filled;
            glowImg.fillMethod = Image.FillMethod.Horizontal;
            glowImg.fillAmount = 0f;
            glowImg.raycastTarget = false;
        }

        /// <summary>
        /// Cria um card de pergunta aprimorado
        /// </summary>
        private void CreateEnhancedQuestionCard(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var questionCard = createGlassCardFunc(_contentContainer, questionCardHeight);
            questionCard.color = QuestionCardColor;

            // Add subtle border glow
            var glowBorder = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image));
            glowBorder.transform.SetParent(questionCard.transform, false);
            glowBorder.transform.SetAsFirstSibling();

            var glowRT = glowBorder.GetComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.offsetMin = new Vector2(-2, -2);
            glowRT.offsetMax = new Vector2(2, 2);

            var glowImg = glowBorder.GetComponent<Image>();
            glowImg.color = new Color32(147, 112, 219, 30);
            glowImg.raycastTarget = false;

            var questionVLG = questionCard.gameObject.AddComponent<VerticalLayoutGroup>();
            questionVLG.padding = new RectOffset(28, 28, 24, 24);
            questionVLG.spacing = 0;
            questionVLG.childAlignment = TextAnchor.MiddleCenter;
            questionVLG.childControlWidth = true;
            questionVLG.childForceExpandWidth = true;
            questionVLG.childControlHeight = false;
            questionVLG.childForceExpandHeight = false;

            var questionFitter = questionCard.gameObject.AddComponent<ContentSizeFitter>();
            questionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _questionText = (TextMeshProUGUI)UiKit.TMP(questionCard.transform, "", questionFontSize,
                Color.white, TextAlignmentOptions.Center);
            _questionText.enableWordWrapping = true;
            _questionText.fontStyle = FontStyles.Bold;
            _questionText.lineSpacing = 10;
        }

        /// <summary>
        /// Cria o container das opções de resposta
        /// </summary>
        private void CreateOptionsContainer()
        {
            _optionsContainer = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            _optionsContainer.transform.SetParent(_contentContainer, false);

            var optionsVLG = _optionsContainer.GetComponent<VerticalLayoutGroup>();
            optionsVLG.spacing = 16;
            optionsVLG.padding = new RectOffset(0, 0, 0, 0);
            optionsVLG.childControlHeight = true;
            optionsVLG.childForceExpandHeight = false;
            optionsVLG.childControlWidth = true;
            optionsVLG.childForceExpandWidth = true;
        }

        /// <summary>
        /// Cria um card de explicação aprimorado
        /// </summary>
        private void CreateEnhancedExplanationCard(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var expCard = createGlassCardFunc(_contentContainer, explanationHeight);
            _explanationCard = expCard.gameObject;
            _explanationCardBackground = expCard;

            var expVLG = expCard.gameObject.AddComponent<VerticalLayoutGroup>();
            expVLG.padding = new RectOffset(32, 32, 28, 28); // Even more padding
            expVLG.spacing = 20; // More spacing between title and text
            expVLG.childControlWidth = true;
            expVLG.childForceExpandWidth = true;
            expVLG.childControlHeight = false;
            expVLG.childForceExpandHeight = false;

            var expFitter = expCard.gameObject.AddComponent<ContentSizeFitter>();
            expFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title with icon
            var titleContainer = new GameObject("TitleContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleContainer.transform.SetParent(expCard.transform, false);

            var titleHLG = titleContainer.GetComponent<HorizontalLayoutGroup>();
            titleHLG.spacing = 10;
            titleHLG.childAlignment = TextAnchor.MiddleLeft;
            titleHLG.childControlWidth = false;
            titleHLG.childForceExpandWidth = false;

            _explanationTitleText = (TextMeshProUGUI)UiKit.TMP(titleContainer.transform, "❌ Incorreto!",
                explanationTitleFontSize, Color.white, TextAlignmentOptions.Left, bold: true);
            _explanationTitleText.name = "ExpTitle";
            _explanationTitleText.enableWordWrapping = false;
            _explanationTitleText.fontStyle = FontStyles.Bold; // Ensure bold
            _explanationTitleText.outlineWidth = 0.15f; // Add outline for visibility
            _explanationTitleText.outlineColor = new Color32(0, 0, 0, 100);

            _explanationText = (TextMeshProUGUI)UiKit.TMP(expCard.transform, "", explanationTextFontSize,
                new Color32(255, 255, 255, 255), TextAlignmentOptions.Left); // Full white for readability
            _explanationText.enableWordWrapping = true;
            _explanationText.lineSpacing = 15; // Much more line spacing (was 10)
            _explanationText.margin = new Vector4(0, 8, 0, 0); // More top margin

            _explanationCard.SetActive(false);
        }

        /// <summary>
        /// Cria o botão "Próxima Pergunta"
        /// </summary>
        private void CreateNextButton(System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            _nextButton = createCTAButtonFunc(_contentContainer.transform, "Próxima Pergunta →", OnNextButtonClicked);

            // Make button taller
            var btnLE = _nextButton.GetComponent<LayoutElement>();
            if (btnLE == null) btnLE = _nextButton.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 60f;
            btnLE.minHeight = 60f;

            // Update text size
            var btnText = _nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = 22;
                btnText.fontStyle = FontStyles.Bold;
            }

            _nextButton.SetActive(false);
        }

        /// <summary>
        /// Inicializa o quiz
        /// </summary>
        private void InitializeQuiz()
        {
            _currentQuizIndex = 0;
            _quizScore = 0;
            _answerSelected = false;
            UpdateUI();
        }

        /// <summary>
        /// Atualiza a interface do usuário com a pergunta atual
        /// </summary>
        private void UpdateUI()
        {
            if (_currentQuizIndex >= SampleData.QuizQuestions.Count)
            {
                ShowQuizResult();
                return;
            }

            _answerSelected = false;

            // Clear previous options
            foreach (Transform child in _optionsContainer.transform)
                Destroy(child.gameObject);
            _optionButtons.Clear();

            _explanationCard.SetActive(false);
            _nextButton.SetActive(false);

            var question = SampleData.QuizQuestions[_currentQuizIndex];

            // Update question number
            _questionNumberLabel.text = $"Pergunta {_currentQuizIndex + 1} de {SampleData.QuizQuestions.Count}";

            // Update score
            _scoreLabel.text = $"{_quizScore} ponto{(_quizScore != 1 ? "s" : "")}";

            // Update progress
            float progress = _currentQuizIndex / Mathf.Max(1f, (float)(SampleData.QuizQuestions.Count - 1));
            AnimateProgress(progress);

            // Update question with fade-in animation
            _questionText.text = question.Question;
            if (_currentAnimation != null) StopCoroutine(_currentAnimation);
            _currentAnimation = StartCoroutine(FadeInQuestion());

            // Create option buttons with staggered animation
            CreateOptionButtons(question);

            OnScoreUpdated?.Invoke(_quizScore, SampleData.QuizQuestions.Count);
        }

        /// <summary>
        /// Anima a barra de progresso
        /// </summary>
        private void AnimateProgress(float targetFill)
        {
            if (_currentAnimation != null) StopCoroutine(_currentAnimation);
            _currentAnimation = StartCoroutine(AnimateProgressCoroutine(targetFill));
        }

        private IEnumerator AnimateProgressCoroutine(float targetFill)
        {
            float startFill = _progressFill.fillAmount;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easedT = Mathf.SmoothStep(0, 1, t);

                _progressFill.fillAmount = Mathf.Lerp(startFill, targetFill, easedT);
                _progressGlow.GetComponent<Image>().fillAmount = _progressFill.fillAmount;

                yield return null;
            }

            _progressFill.fillAmount = targetFill;
            _progressGlow.GetComponent<Image>().fillAmount = targetFill;
        }

        /// <summary>
        /// Anima o fade-in da pergunta
        /// </summary>
        private IEnumerator FadeInQuestion()
        {
            _questionText.alpha = 0f;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _questionText.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            _questionText.alpha = 1f;
        }

        /// <summary>
        /// Cria os botões das opções de resposta
        /// </summary>
        private void CreateOptionButtons(QuizQuestion question)
        {
            for (int i = 0; i < question.Options.Count; i++)
            {
                int optionIndex = i;
                var optionButton = CreateEnhancedOptionButton(question.Options[i], optionIndex, question);
                _optionButtons.Add(optionButton);

                // Staggered fade-in
                StartCoroutine(FadeInOption(optionButton, i * 0.1f));
            }
        }

        /// <summary>
        /// Cria um botão de opção aprimorado
        /// </summary>
        private GameObject CreateEnhancedOptionButton(string optionText, int optionIndex, QuizQuestion question)
        {
            var optionCard = UiKit.CreateCard(_optionsContainer.transform, new Vector2(0, optionHeight),
                OptionColor, 20f, glass: false);

            // Add shadow/depth effect
            var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowGO.transform.SetParent(optionCard.transform, false);
            shadowGO.transform.SetAsFirstSibling();

            var shadowRT = shadowGO.GetComponent<RectTransform>();
            shadowRT.anchorMin = Vector2.zero;
            shadowRT.anchorMax = Vector2.one;
            shadowRT.offsetMin = new Vector2(0, -4);
            shadowRT.offsetMax = new Vector2(0, -4);

            var shadowImg = shadowGO.GetComponent<Image>();
            shadowImg.color = ShadowColor;
            shadowImg.raycastTarget = false;

            var btn = optionCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = optionCard;

            // Enhanced button colors
            var colors = btn.colors;
            colors.normalColor = OptionColor;
            colors.highlightedColor = new Color(0.95f, 0.95f, 1f, 1f);
            colors.pressedColor = OptionHoverColor;
            colors.disabledColor = DisabledOptionColor;
            btn.colors = colors;

            // Option letter badge
            var badgeContainer = new GameObject("BadgeContainer", typeof(RectTransform));
            badgeContainer.transform.SetParent(optionCard.transform, false);

            var badgeRT = badgeContainer.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0, 0.5f);
            badgeRT.anchorMax = new Vector2(0, 0.5f);
            badgeRT.pivot = new Vector2(0, 0.5f);
            badgeRT.anchoredPosition = new Vector2(20, 0);
            badgeRT.sizeDelta = new Vector2(40, 40);

            var badgeCircle = new GameObject("BadgeCircle", typeof(RectTransform), typeof(Image));
            badgeCircle.transform.SetParent(badgeContainer.transform, false);

            var circleRT = badgeCircle.GetComponent<RectTransform>();
            circleRT.anchorMin = Vector2.zero;
            circleRT.anchorMax = Vector2.one;
            circleRT.offsetMin = Vector2.zero;
            circleRT.offsetMax = Vector2.zero;

            var circleImg = badgeCircle.GetComponent<Image>();
            circleImg.color = new Color32(103, 80, 164, 80); // More opaque badge background
            circleImg.raycastTarget = false;

            string[] letters = { "A", "B", "C", "D" };
            var badgeLetter = UiKit.TMP(badgeCircle.transform,
                optionIndex < letters.Length ? letters[optionIndex] : (optionIndex + 1).ToString(),
                20, new Color32(255, 255, 255, 255), TextAlignmentOptions.Center, bold: true); // White text for contrast
            badgeLetter.raycastTarget = false;
            badgeLetter.fontStyle = FontStyles.Bold;

            var letterRT = badgeLetter.rectTransform;
            letterRT.anchorMin = Vector2.zero;
            letterRT.anchorMax = Vector2.one;
            letterRT.offsetMin = Vector2.zero;
            letterRT.offsetMax = Vector2.zero;

            // Option text
            var optionTMP = UiKit.TMP(optionCard.transform, optionText, optionFontSize,
                OptionTextColor, TextAlignmentOptions.Left, bold: false);
            optionTMP.enableWordWrapping = true;
            optionTMP.raycastTarget = false;
            optionTMP.margin = new Vector4(0, 0, 0, 0);

            var optionRT = optionTMP.rectTransform;
            optionRT.anchorMin = Vector2.zero;
            optionRT.anchorMax = Vector2.one;
            optionRT.offsetMin = new Vector2(76, 12);
            optionRT.offsetMax = new Vector2(-20, -12);

            btn.onClick.AddListener(() => OnOptionSelected(optionIndex, question, optionCard.gameObject));

            return optionCard.gameObject;
        }

        /// <summary>
        /// Anima o fade-in das opções
        /// </summary>
        private IEnumerator FadeInOption(GameObject option, float delay)
        {
            var canvasGroup = option.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = option.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Manipula a seleção de uma opção
        /// </summary>
        private void OnOptionSelected(int selectedIndex, QuizQuestion question, GameObject selectedButton)
        {
            if (_answerSelected) return;
            _answerSelected = true;

            bool isCorrect = selectedIndex == question.CorrectAnswerIndex;

            if (isCorrect)
            {
                _quizScore++;
                StartCoroutine(PlayCorrectAnimation(selectedButton));
            }
            else
            {
                StartCoroutine(PlayWrongAnimation(selectedButton));
            }

            // Update option colors
            UpdateOptionColors(selectedIndex, question.CorrectAnswerIndex);

            // Show explanation with animation
            ShowExplanation(question.Explanation, isCorrect);

            // Update score
            _scoreLabel.text = $"{_quizScore} ponto{(_quizScore != 1 ? "s" : "")}";

            // Show next button with delay
            StartCoroutine(ShowNextButtonDelayed());

            // Trigger events
            OnQuestionAnswered?.Invoke(question, isCorrect);
            OnScoreUpdated?.Invoke(_quizScore, SampleData.QuizQuestions.Count);
        }

        /// <summary>
        /// Animação de resposta correta
        /// </summary>
        private IEnumerator PlayCorrectAnimation(GameObject button)
        {
            var originalScale = button.transform.localScale;

            // Pulse effect
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.1f;
                button.transform.localScale = originalScale * scale;
                yield return null;
            }

            button.transform.localScale = originalScale;
        }

        /// <summary>
        /// Animação de resposta errada
        /// </summary>
        private IEnumerator PlayWrongAnimation(GameObject button)
        {
            var originalPos = button.transform.localPosition;

            // Shake effect
            float elapsed = 0f;
            float duration = 0.4f;
            float magnitude = 5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = originalPos.x + Random.Range(-magnitude, magnitude) * (1f - elapsed / duration);
                button.transform.localPosition = new Vector3(x, originalPos.y, originalPos.z);
                yield return null;
            }

            button.transform.localPosition = originalPos;
        }

        /// <summary>
        /// Mostra a explicação com animação
        /// </summary>
        private void ShowExplanation(string explanation, bool wasCorrect)
        {
            _explanationTitleText.text = wasCorrect ? "✅ Correto!" : "❌ Incorreto!";
            _explanationText.text = explanation;
            _explanationCardBackground.color = wasCorrect ? ExplanationCorrectBg : ExplanationWrongBg;

            StartCoroutine(FadeInExplanation());
        }

        private IEnumerator FadeInExplanation()
        {
            var canvasGroup = _explanationCard.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _explanationCard.AddComponent<CanvasGroup>();

            _explanationCard.SetActive(true);
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Mostra o botão próximo com delay
        /// </summary>
        private IEnumerator ShowNextButtonDelayed()
        {
            yield return new WaitForSeconds(0.5f);

            var canvasGroup = _nextButton.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _nextButton.AddComponent<CanvasGroup>();

            _nextButton.SetActive(true);
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Atualiza as cores das opções após a seleção
        /// </summary>
        private void UpdateOptionColors(int selectedIndex, int correctIndex)
        {
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                var option = _optionButtons[i];
                var img = option.GetComponent<Image>();
                var txt = option.GetComponentInChildren<TextMeshProUGUI>();
                var button = option.GetComponent<Button>();
                var badge = option.transform.Find("BadgeContainer/BadgeCircle")?.GetComponent<Image>();

                if (button)
                    button.interactable = false;

                if (i == correctIndex)
                {
                    img.color = CorrectOptionColor;
                    txt.color = Color.white;
                    if (badge) badge.color = new Color32(255, 255, 255, 80);
                }
                else if (i == selectedIndex)
                {
                    img.color = WrongOptionColor;
                    txt.color = Color.white;
                    if (badge) badge.color = new Color32(255, 255, 255, 80);
                }
                else
                {
                    img.color = DisabledOptionColor;
                    txt.color = new Color32(120, 120, 120, 255);
                    if (badge) badge.color = new Color32(100, 100, 100, 80);
                }
            }
        }

        /// <summary>
        /// Manipula o clique no botão "Próxima Pergunta"
        /// </summary>
        private void OnNextButtonClicked()
        {
            _currentQuizIndex++;
            StartCoroutine(TransitionToNextQuestion());
        }

        /// <summary>
        /// Transição suave para a próxima pergunta
        /// </summary>
        private IEnumerator TransitionToNextQuestion()
        {
            // Fade out current content
            var contentCanvasGroup = _contentContainer.GetComponent<CanvasGroup>();
            if (contentCanvasGroup == null) contentCanvasGroup = _contentContainer.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                contentCanvasGroup.alpha = Mathf.Lerp(1f, 0.5f, elapsed / duration);
                yield return null;
            }

            // Update UI
            UpdateUI();

            // Fade in
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                contentCanvasGroup.alpha = Mathf.Lerp(0.5f, 1f, elapsed / duration);
                yield return null;
            }

            contentCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Mostra o resultado final do quiz
        /// </summary>
        private void ShowQuizResult()
        {
            StartCoroutine(ShowQuizResultCoroutine());
        }

        private IEnumerator ShowQuizResultCoroutine()
        {
            // Fade out
            var contentCanvasGroup = _contentContainer.GetComponent<CanvasGroup>();
            if (contentCanvasGroup == null) contentCanvasGroup = _contentContainer.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                contentCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            // Clear all content
            foreach (Transform child in _contentContainer)
                Destroy(child.gameObject);

            // Create result screen
            CreateResultScreen();

            // Fade in
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                contentCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            contentCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Cria a tela de resultado
        /// </summary>
        private void CreateResultScreen()
        {
            AddSpacer(_contentContainer, 40f);

            // Trophy icon
            var trophyTMP = UiKit.TMP(_contentContainer, "🏆", 80,
                new Color32(255, 215, 0, 255), TextAlignmentOptions.Center);
            trophyTMP.margin = new Vector4(0, 0, 0, 20);

            // Title
            var titleTMP = UiKit.TMP(_contentContainer, "Quiz Concluído!", 40,
                Color.white, TextAlignmentOptions.Center, bold: true);
            titleTMP.margin = new Vector4(0, 0, 0, 32);

            // Result card
            var resultCard = UiKit.CreateCard(_contentContainer, new Vector2(0, 260f),
                new Color(0f, 0f, 0f, 0.4f), 24f, glass: true); // Darker background

            var resultVLG = resultCard.gameObject.AddComponent<VerticalLayoutGroup>();
            resultVLG.padding = new RectOffset(32, 32, 32, 32);
            resultVLG.spacing = 20;
            resultVLG.childAlignment = TextAnchor.MiddleCenter;
            resultVLG.childControlWidth = true;
            resultVLG.childForceExpandWidth = true;
            resultVLG.childControlHeight = false;
            resultVLG.childForceExpandHeight = false;

            // Score display
            var scoreTMP = UiKit.TMP(resultCard.transform,
                $"{_quizScore}",
                72, new Color32(147, 112, 219, 255), TextAlignmentOptions.Center, bold: true);
            scoreTMP.fontStyle = FontStyles.Bold;

            var outOfTMP = UiKit.TMP(resultCard.transform,
                $"de {SampleData.QuizQuestions.Count} perguntas",
                26, Color.white, TextAlignmentOptions.Center); // Larger and full white

            AddSpacer(resultCard.transform, 12f);

            // Performance message
            string message = GetResultMessage();
            var msgTMP = UiKit.TMP(resultCard.transform, message, 22, // Larger font
                Color.white, TextAlignmentOptions.Center); // Full white
            msgTMP.enableWordWrapping = true;
            msgTMP.lineSpacing = 8; // More line spacing

            // Save high score
            SaveHighScore();

            // High score display if achieved
            int highScore = GetHighScore();
            if (_quizScore >= highScore && _quizScore > 0)
            {
                AddSpacer(resultCard.transform, 8f);
                var highScoreTMP = UiKit.TMP(resultCard.transform, "🎉 Novo Recorde!", 18,
                    new Color32(255, 215, 0, 255), TextAlignmentOptions.Center, bold: true);
            }

            AddSpacer(_contentContainer, 32f);

            // Restart button
            var restartCard = UiKit.CreateCard(_contentContainer, new Vector2(0, 64f),
                Color.white, 20f, glass: false);

            var restartBtn = restartCard.gameObject.AddComponent<Button>();
            restartBtn.targetGraphic = restartCard;
            restartBtn.onClick.AddListener(RestartQuiz);

            var colors = restartBtn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color32(147, 112, 219, 100);
            restartBtn.colors = colors;

            var restartTMP = UiKit.TMP(restartCard.transform, "🔄 Jogar Novamente", 24,
                new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            restartTMP.raycastTarget = false;

            var restartRT = restartTMP.rectTransform;
            restartRT.anchorMin = Vector2.zero;
            restartRT.anchorMax = Vector2.one;
            restartRT.offsetMin = new Vector2(20, 12);
            restartRT.offsetMax = new Vector2(-20, -12);

            AddSpacer(_contentContainer, 60f);

            OnQuizCompleted?.Invoke(_quizScore);
        }

        /// <summary>
        /// Obtém a mensagem de resultado baseada na performance
        /// </summary>
        private string GetResultMessage()
        {
            float percentage = (float)_quizScore / SampleData.QuizQuestions.Count;

            if (percentage >= 1.0f)
                return "Perfeito! Você é um verdadeiro expert em tecnologia retrô! 🌟";
            else if (percentage >= 0.8f)
                return "Excelente! Você tem um ótimo conhecimento sobre tecnologia retrô! 🎯";
            else if (percentage >= 0.6f)
                return "Muito bom! Continue explorando para aprender ainda mais! 📚";
            else if (percentage >= 0.4f)
                return "Bom trabalho! Que tal revisar alguns conceitos? 💡";
            else
                return "Continue praticando! Explore mais sobre as peças do museu! 🚀";
        }

        /// <summary>
        /// Salva a pontuação máxima
        /// </summary>
        private void SaveHighScore()
        {
            int previousHigh = PlayerPrefs.GetInt("RetroTech_HighScore", 0);
            if (_quizScore > previousHigh)
            {
                PlayerPrefs.SetInt("RetroTech_HighScore", _quizScore);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Reinicia o quiz
        /// </summary>
        public void RestartQuiz()
        {
            _currentQuizIndex = 0;
            _quizScore = 0;
            _answerSelected = false;

            // Clear result content
            foreach (Transform child in _contentContainer)
                Destroy(child.gameObject);

            // Recreate quiz - need to store creation functions
            // For now, just reinitialize
            InitializeQuiz();
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
        public void UpdateVisualSettings(float headerH, float questionH, float optionH, float explanationH,
                                       int titleSize, int scoreSize, int questionSize, int optionSize)
        {
            headerHeight = headerH;
            questionCardHeight = questionH;
            optionHeight = optionH;
            explanationHeight = explanationH;
            pageTitleFontSize = titleSize;
            scoreFontSize = scoreSize;
            questionFontSize = questionSize;
            optionFontSize = optionSize;
        }

        /// <summary>
        /// Obtém a pontuação atual do quiz
        /// </summary>
        public int GetCurrentScore()
        {
            return _quizScore;
        }

        /// <summary>
        /// Obtém o índice da pergunta atual
        /// </summary>
        public int GetCurrentQuestionIndex()
        {
            return _currentQuizIndex;
        }

        /// <summary>
        /// Obtém a pontuação máxima salva
        /// </summary>
        public int GetHighScore()
        {
            return PlayerPrefs.GetInt("RetroTech_HighScore", 0);
        }

        /// <summary>
        /// Adiciona espaço vertical
        /// </summary>
        private void AddSpacer(Transform parent, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            if (_currentAnimation != null)
                StopCoroutine(_currentAnimation);

            OnScoreUpdated = null;
            OnQuizCompleted = null;
            OnQuestionAnswered = null;
        }

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            headerHeight = Mathf.Max(60f, headerHeight);
            questionCardHeight = Mathf.Max(100f, questionCardHeight);
            optionHeight = Mathf.Max(50f, optionHeight);
            explanationHeight = Mathf.Max(80f, explanationHeight);
            pageTitleFontSize = Mathf.Max(12, pageTitleFontSize);
            scoreFontSize = Mathf.Max(12, scoreFontSize);
            questionFontSize = Mathf.Max(12, questionFontSize);
            optionFontSize = Mathf.Max(12, optionFontSize);
            questionNumberFontSize = Mathf.Max(10, questionNumberFontSize);
            explanationTitleFontSize = Mathf.Max(12, explanationTitleFontSize);
            explanationTextFontSize = Mathf.Max(10, explanationTextFontSize);
        }
#endif
        #endregion
    }
}