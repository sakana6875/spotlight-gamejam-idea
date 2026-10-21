using Spotlight.Domain.Story;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spotlight.Story
{
    /// <summary>
    /// 把剧情展示数据画到场景 UI 上。不推进规则，只转发按钮。
    /// </summary>
    public sealed class StoryDialogueView : MonoBehaviour
    {
        [SerializeField] private StoryViewEntry _entry;
        [SerializeField] private GameObject _storyRoot;
        [SerializeField] private Image _background;
        [SerializeField] private Image _portrait;
        [SerializeField] private Image _portraitSecondary;
        [SerializeField] private TMP_Text _speakerName;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private float _charactersPerSecond = 30f;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _namePanel;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private GameObject _choicePanel;
        [SerializeField] private Button _choiceTemplate;

        private StorySessionHost _host;
        private readonly StoryTypewriter _typewriter = new StoryTypewriter();
        private bool _isTypingLine;
        private bool _isNaming;

        private void Start()
        {
            if (_entry == null || _entry.Host == null)
            {
                Debug.LogError("剧情 UI 没有绑到 StoryViewEntry。把它和本脚本放在同一个 Canvas 上。", this);
                return;
            }

            _host = _entry.Host;
            _host.PresentationChanged += Show;
            Button clickArea = _background == null ? null : _background.GetComponent<Button>();
            if (clickArea != null)
            {
                clickArea.onClick.AddListener(OnScreenClicked);
            }

            PlacePortraitsBehindDialogue();
            Show(_host.Current);
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.PresentationChanged -= Show;
            }
        }

        private void Update()
        {
            if (!_isTypingLine || _body == null || _typewriter.IsComplete)
            {
                return;
            }

            _typewriter.Reveal(Time.unscaledDeltaTime, _charactersPerSecond);
            _body.text = _typewriter.VisibleText;
            if (_typewriter.IsComplete)
            {
                _isTypingLine = false;
            }
        }


        /// <summary>
        /// 点击屏幕继续。打字未完成时只补全当前句；改名、选项和进化之地玩法演出期间忽略。
        /// </summary>
        public void OnScreenClicked()
        {
            if (_host == null)
            {
                return;
            }

            StoryPresentation current = _host.Current;
            if (current.Kind == StoryWaitKind.Choices || _isNaming)
            {
                return;
            }

            if (current.NodeId == StorySession.StartNodeId && !_host.HasPlayerName)
            {
                if (!_typewriter.Confirm())
                {
                    if (_body != null)
                    {
                        _body.text = _typewriter.VisibleText;
                    }

                    _isTypingLine = false;
                    return;
                }

                _isNaming = true;
                _isTypingLine = false;
                if (_namePanel != null)
                {
                    _namePanel.SetActive(true);
                }

                if (_continueButton != null)
                {
                    _continueButton.gameObject.SetActive(false);
                }

                return;
            }

            if (current.Kind == StoryWaitKind.Line)
            {
                if (!_typewriter.Confirm())
                {
                    if (_body != null)
                    {
                        _body.text = _typewriter.VisibleText;
                    }

                    _isTypingLine = false;
                    return;
                }

                _host.Advance();
                return;
            }

            if (current.Kind == StoryWaitKind.Event)
            {
                _host.CompleteCurrentEvent();
            }
        }

        /// <summary>
        /// 改名确认。开场问名字之后写入名字，再进入带名字的欢迎句。
        /// </summary>
        public void OnNameConfirmed()
        {
            if (_host == null || _nameInput == null)
            {
                return;
            }

            _host.SetPlayerName(_nameInput.text);
            _isNaming = false;
            if (_host.Current.NodeId == StorySession.StartNodeId)
            {
                if (!_host.Advance())
                {
                    Debug.LogError("改名后没有进入欢迎句。", this);
                }

                return;
            }

            if (!_host.CompleteCurrentEvent())
            {
                Debug.LogError("改名确认没有推进剧情。当前不是可完成的演出，或场景切换还没结束。", this);
            }
        }

        private void Show(StoryPresentation presentation)
        {
            // 玩法事实可能在同一次通知里完成并发布下一句。后执行的旧画面不能把新旁白藏掉。
            if (_host != null && !ReferenceEquals(presentation, _host.Current))
            {
                return;
            }

            bool hideForDemo = (_host != null && (_host.IsDemoPlaybackActive || _host.IsNarrationHeld)) ||
                presentation.EventId == StorySessionHost.Demo1PlayEventId ||
                presentation.EventId == StorySessionHost.Demo3EnterEventId;
            if (_storyRoot != null)
            {
                _storyRoot.SetActive(!hideForDemo);
            }

            // BackGround 和 StoryRoot 同级。只藏对话框时，全屏透明点击层仍会吃掉 Demo1 的按钮。
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = !hideForDemo;
            }

            if (hideForDemo)
            {
                _isTypingLine = false;
                if (_continueButton != null)
                {
                    _continueButton.gameObject.SetActive(false);
                }

                return;
            }
            if (presentation.NodeId != StorySession.StartNodeId)
            {
                _isNaming = false;
            }


            bool naming = _isNaming && presentation.NodeId == StorySession.StartNodeId && !_host.HasPlayerName;
            bool choosing = presentation.Kind == StoryWaitKind.Choices;
            if (_namePanel != null)
            {
                _namePanel.SetActive(naming);
            }

            if (_choicePanel != null)
            {
                _choicePanel.SetActive(choosing);
            }

            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(!naming && !choosing && presentation.Kind != StoryWaitKind.Ended);
            }

            if (_speakerName != null)
            {
                _speakerName.text = presentation.SpeakerNames.Length == 0
                    ? string.Empty
                    : string.Join(" / ", presentation.SpeakerNames);
            }

            if (_body != null)
            {
                ApplyBody(presentation);
            }

            ShowPortrait(_portrait, presentation, 0);
            ShowPortrait(_portraitSecondary, presentation, 1);
            RebuildChoices(presentation);
        }

        private void ApplyBody(StoryPresentation presentation)
        {
            string body = BuildBody(presentation);
            bool typeLine = presentation.Kind == StoryWaitKind.Line && _charactersPerSecond > 0f && body.Length > 0;
            _typewriter.Restart(body);
            if (!typeLine)
            {
                _typewriter.Confirm();
                _isTypingLine = false;
                _body.text = body;
                return;
            }

            _isTypingLine = true;
            _body.text = string.Empty;
        }


        private static string BuildBody(StoryPresentation presentation)
        {
            if (presentation.Kind == StoryWaitKind.Blocked)
            {
                return presentation.BlockReason;
            }

            if (presentation.Kind == StoryWaitKind.Ended && presentation.Text.Length == 0)
            {
                return "剧情到这里结束了。";
            }

            if (presentation.NodeId == "music_010")
            {
                return presentation.Text + "\n高度 " + presentation.Demo1FinalHeight;
            }

            return presentation.Text;
        }

        private static void ShowPortrait(Image image, StoryPresentation presentation, int index)
        {
            if (image == null)
            {
                return;
            }

            bool visible = presentation.Portraits != null
                && index < presentation.Portraits.Length
                && presentation.Portraits[index] != null;
            image.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            image.sprite = presentation.Portraits[index];
            image.raycastTarget = false;
            // 画布参考 PPU 和立绘导入 PPU 都是 100，SetNativeSize 就是图片像素尺寸。
            image.SetNativeSize();
        }

        private void PlacePortraitsBehindDialogue()
        {
            if (_body == null)
            {
                return;
            }

            RectTransform dialogue = _body.transform.parent as RectTransform;
            if (dialogue == null || dialogue.parent == null)
            {
                return;
            }

            // 子物体总是画在父物体图片上面。立绘必须和对话框同级，并且排在它前面。
            PlaceBehind(dialogue, _portrait);
            PlaceBehind(dialogue, _portraitSecondary);
        }

        private static void PlaceBehind(RectTransform dialogue, Image portrait)
        {
            if (portrait == null)
            {
                return;
            }

            portrait.raycastTarget = false;
            Transform parent = dialogue.parent;
            if (portrait.transform.parent != parent)
            {
                portrait.transform.SetParent(parent, true);
            }

            int dialogueIndex = dialogue.GetSiblingIndex();
            if (portrait.transform.GetSiblingIndex() > dialogueIndex)
            {
                portrait.transform.SetSiblingIndex(dialogueIndex);
            }
        }

        private void RebuildChoices(StoryPresentation presentation)
        {
            if (_choicePanel == null || _choiceTemplate == null)
            {
                return;
            }

            for (int index = _choicePanel.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = _choicePanel.transform.GetChild(index);
                if (child == _choiceTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }

            if (presentation.Kind != StoryWaitKind.Choices)
            {
                return;
            }

            for (int index = 0; index < presentation.Choices.Length; index++)
            {
                StoryChoice choice = presentation.Choices[index];
                Button button = Instantiate(_choiceTemplate, _choicePanel.transform);
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    if (_body != null && _body.font != null)
                    {
                        label.font = _body.font;
                    }

                    label.text = choice.Text;
                }

                string nodeId = choice.NodeId;
                button.onClick.AddListener(() => _host.SelectChoice(nodeId));
            }
        }
    }
}
