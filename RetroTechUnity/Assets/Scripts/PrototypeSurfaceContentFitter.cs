using UnityEngine;
using UnityEngine.UI;

namespace RetroTech
{
    /// <summary>
    /// Keeps prototype scroll content at least as tall as its viewport while distributing
    /// extra vertical space with invisible flexible spacers so cards do not cling to the top
    /// on tall screens.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class PrototypeSurfaceContentFitter : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _viewport;

        [SerializeField]
        private LayoutElement _layoutElement;

        [SerializeField, Min(0f)]
        private float _topFlexWeight = 0.5f;

        [SerializeField, Min(0f)]
        private float _bottomFlexWeight = 1.5f;

        private VerticalLayoutGroup _layoutGroup;
        private RectTransform _topSpacer;
        private RectTransform _bottomSpacer;
        private float _lastAppliedMinHeight = -1f;

        public void Initialize(RectTransform viewport, LayoutElement layoutElement)
        {
            _viewport = viewport;
            _layoutElement = layoutElement;
            EnsureDependencies();
            EnsureSpacers();
            RefreshLayout(true);
        }

        private void Awake()
        {
            EnsureDependencies();
            EnsureSpacers();
        }

        private void OnEnable()
        {
            RefreshLayout(true);
        }

        private void Update()
        {
            RefreshLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }

        private void OnTransformChildrenChanged()
        {
            if (EnsureSpacers())
                RefreshLayout(true);
        }

        private void EnsureDependencies()
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<VerticalLayoutGroup>();

            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            if (_layoutElement != null)
            {
                if (_layoutElement.flexibleHeight <= 0f)
                    _layoutElement.flexibleHeight = 1f;
                if (_layoutElement.preferredHeight != 0f)
                    _layoutElement.preferredHeight = 0f;
            }
        }

        private bool EnsureSpacers()
        {
            bool changed = false;

            if (_topSpacer == null)
            {
                _topSpacer = FindExistingSpacer("TopFlexibleSpace");
                if (_topSpacer == null)
                {
                    _topSpacer = CreateSpacer("TopFlexibleSpace", _topFlexWeight);
                    changed = true;
                }
            }

            if (_bottomSpacer == null)
            {
                _bottomSpacer = FindExistingSpacer("BottomFlexibleSpace");
                if (_bottomSpacer == null)
                {
                    _bottomSpacer = CreateSpacer("BottomFlexibleSpace", _bottomFlexWeight);
                    changed = true;
                }
            }

            if (_topSpacer != null)
                _topSpacer.SetSiblingIndex(0);

            if (_bottomSpacer != null)
                _bottomSpacer.SetAsLastSibling();

            return changed;
        }

        private RectTransform FindExistingSpacer(string name)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private RectTransform CreateSpacer(string name, float flexibleWeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(transform, false);
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 0f;
            layout.preferredHeight = 0f;
            layout.flexibleHeight = Mathf.Max(0f, flexibleWeight);

            return go.GetComponent<RectTransform>();
        }

        private void RefreshLayout(bool force = false)
        {
            if (_viewport == null || _layoutElement == null)
                return;

            float viewportHeight = _viewport.rect.height;
            if (viewportHeight <= 0f)
                return;

            float verticalPadding = _layoutGroup != null ? _layoutGroup.padding.top + _layoutGroup.padding.bottom : 0f;
            float targetMinHeight = Mathf.Max(0f, viewportHeight - verticalPadding);

            if (force || !Mathf.Approximately(_lastAppliedMinHeight, targetMinHeight))
            {
                _layoutElement.minHeight = targetMinHeight;
                _lastAppliedMinHeight = targetMinHeight;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _topFlexWeight = Mathf.Max(0f, _topFlexWeight);
            _bottomFlexWeight = Mathf.Max(0f, _bottomFlexWeight);

            if (_topSpacer != null)
            {
                var topLayout = _topSpacer.GetComponent<LayoutElement>();
                topLayout.flexibleHeight = _topFlexWeight;
            }

            if (_bottomSpacer != null)
            {
                var bottomLayout = _bottomSpacer.GetComponent<LayoutElement>();
                bottomLayout.flexibleHeight = _bottomFlexWeight;
            }

            RefreshLayout(true);
        }
#endif
    }
}
