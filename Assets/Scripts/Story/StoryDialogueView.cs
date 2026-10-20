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
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _namePanel;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private GameObject _choicePanel;
        [SerializeField] private Button _choiceTemplate;

        private StorySessionHost _host;

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

        /// <summary>
        /// 点击屏幕继续。改名、选项和进化之地玩法演出期间忽略，避免把输入点掉或跳过尚未发生的事实。
        /// </summary>
        public void OnScreenClicked()
        {
            if (_host == null)
            {
                return;
            }

            StoryPresentation current = _host.Current;
            if (current.Kind == StoryWaitKind.Choices || current.EventId == "intro_002" ||
                StorySessionHost.IsDemo2GameplayEvent(current.EventId))
            {
                return;
            }

            if (current.Kind == StoryWaitKind.Line)
            {
                _host.Advance();
                return;
            }

            if (current.Kind == StoryWaitKind.Event)
            {
                _host.CompleteCurrentEvent();
            }
        }

        /// <summary>
        /// 改名确认按钮。先写入名字，再完成改名演出。
        /// </summary>
        public void OnNameConfirmed()
        {
            if (_host == null || _nameInput == null)
            {
                return;
            }

            _host.SetPlayerName(_nameInput.text);
            if (!_host.CompleteCurrentEvent())
            {
                Debug.LogError("改名确认没有推进剧情。当前不是可完成的演出，或场景切换还没结束。", this);
            }
        }

        private void Show(StoryPresentation presentation)
        {
            bool hideForDemo = presentation.EventId == StorySessionHost.Demo1PlayEventId ||
                StorySessionHost.IsDemo2GameplayEvent(presentation.EventId);
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
                if (_continueButton != null)
                {
                    _continueButton.gameObject.SetActive(false);
                }

                return;
            }

            bool naming = presentation.EventId == "intro_002";
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
                _body.text = BuildBody(presentation);
            }

            ShowPortrait(_portrait, presentation, 0);
            ShowPortrait(_portraitSecondary, presentation, 1);
            RebuildChoices(presentation);
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
