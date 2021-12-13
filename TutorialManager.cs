using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using DG.Tweening;
using UnityEngine.Events;
using Lean.Gui;

public class TutorialManager : MonoBehaviour
{
    public static UnityAction endEvent;
    static TutorialManager instance;
    public static TutorialManager Instance => instance;
    public static float enemyCompulsionReinforce = 1f;

    // <튜토리얼 순번 0부터 시작. 그래서 초기값 -1>
    static int step = -1;
    public static int Step => step;
    static int maxStep = 43;
    static bool init = false;
    [Header("카메라 조정")]
    public Canvas canvas;


    [Header("강조")]

    [SerializeField] Image darkboard;
    [SerializeField] float darkTweenTime;

    //Dictionary 사용 이유 순서가 연속적이란 보장이 없음 1 2 4 6 7 10 이런식일 수 있음.
    [Header("튜토리얼 대화창 정보")]
    [SerializeField] Button textBoardBtn;
    [SerializeField] RectTransform textBoardTr;
    [SerializeField] Text tutorialText;
    [SerializeField] List<TutorialTextBoard> contents;
    Dictionary<int, TutorialTextBoard> contentDB;

    [Header("튜토리얼의 쓰이는 패널 or 버튼")]
    [SerializeField] List<TutorialPanelList> panels;
    Dictionary<int, TutorialPanelList> panelListDB;


    //이 변수는 순서가 연속이란 보장이 있음 무조건 1 2 3 4 5 6
    [Header("유니티 액션 인스펙터에 노출되는지 확인용")]
    [SerializeField] List<UnityAction> tutorialEvents;

    [Header("기재 사항. 로직적인 사용은 하지 않는 정보")]
    [SerializeField] List<TutorialTooltip> memoboard;

    private void Awake()
    {
        // 저장 데이터 자체적으로 불러옴
        // 불러온 값으로 step 초기화
        if (GameManager.instance.IsNewGame)
        {
            if (!init)
                step = -1;
        }
        else
        {
            if (!init)
                step = SaveSystem.GetInt("tutorial_step", -1);
        }
        if (step > maxStep)
        {
            instance = null;
            Destroy(gameObject);
            return;
        }

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (!init)
        {
            TutorialEventInit();
        }
        else
        {
            Instance.canvas.renderMode = canvas.renderMode;
            Instance.canvas.worldCamera = canvas.worldCamera;
        }

        Instance.contentDB = new Dictionary<int, TutorialTextBoard>();
        foreach (var content in contents)
        {
            Instance.contentDB.Add(content.step, content);
        }

        Instance.panelListDB = new Dictionary<int, TutorialPanelList>();
        foreach (var panel in panels)
        {
            Instance.panelListDB.Add(panel.step, panel);
            foreach(var tutorialPanel in panel.panel_list)
            {
                tutorialPanel.siblingIndexList = new List<int>();
                foreach(var child in tutorialPanel.childList)
                {
                    tutorialPanel.siblingIndexList.Add(child.transform.GetSiblingIndex());
                }
            }
        }

        if (!Application.isEditor)
        {
            contents.Clear();
            contents = null;
            panels.Clear();
            panels = null;
            memoboard.Clear();
            memoboard = null;
        }
        else
        {
            Instance.contents = contents;
            Instance.panels = panels;
        }

        if (!init)
        {
            init = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void TutorialEventInit()
    {
        tutorialEvents = new List<UnityAction>();
        // step = 0
        tutorialEvents.Add(() => {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            textBoardBtn.interactable = true;
            var teamBtn = panel_list.panel_list[0].parent.GetComponent<TopMenuButton>();
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.gameObject.SetActive(false);
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                teamBtn.tutorialEvent = () =>
                {
                    RollBack(Step);
                    teamBtn.tutorialEvent = () =>
                    {
                        teamBtn.tutorialEvent = null;
                        Next();
                    };
                };
                teamBtn.OnClickButton();
            });
        });

        // step = 1
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var aiBtn = panel_list.panel_list[0].childList[0].GetComponent<SubMenuButton>();
            aiBtn.tutorialEvent = () => { RollBack(Step); aiBtn.tutorialEvent = null; };

            var tradeTab = panel_list.panel_list[1].parent.GetComponent<TabChangeButton>();
            tradeTab.tutorialEvent = () =>
            {
                tradeTab.tutorialEvent = null;
                tradeTab.OnClicked();
                Next();
            };
        });

        // step = 2
        tutorialEvents.Add(() =>
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                Next();
            });
        });

        // step = 3 
        tutorialEvents.Add(tutorialEvents[2]);

        // step = 4 
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var aiGamble5Btn = panel_list.panel_list[0].childList[0].GetComponent<AIGambleButton>();
            aiGamble5Btn.tutorialEvent = () =>
            {
                aiGamble5Btn.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        // step = 5 패널창
        tutorialEvents.Add(() =>
        {
            darkboard.gameObject.SetActive(false);
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            var aiGamblePanel = panel_list.panel_list[0].parent.GetComponent<AIGamblePanelController>();
            var gambleAnimPanel = panel_list.panel_list[1].parent.GetComponent<GambleAnimationPanelController>();
            aiGamblePanel.tutorialEvent = () =>
            {

                RollBack(Step);
                aiGamblePanel.OnClickFinalCheckOKButton();
                aiGamblePanel.tutorialEvent = null;
                gambleAnimPanel.tutorialEvent = () =>
                {
                    gambleAnimPanel.tutorialEvent = null;
                    LobbyCanvasManager.instance.backButton.AddEventOnButton();
                    LobbyCanvasManager.instance.ChangeMenu(LobbyCanvasManager.MenuState.Main);
                    darkboard.gameObject.SetActive(true);
                    Next();
                };
            };
        });

        // step = 6
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.8f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            var sallBtn = panel_list.panel_list[0].childList[0].GetComponent<MenuTransitionButton>();
            sallBtn.tutorialEvent = () =>
            {
                sallBtn.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        // step = 7 그냥 대화창
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.interactable = false;
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                Next();
            });
        });

        // step = 8 출전 유닛 UI 강조
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            //조건은 출전 패널에서 정하고 결과만 출력하는 이벤트 작성
            var sallyPanelController = panel_list.panel_list[1].parent.GetComponent<SallyPanelController>();
            sallyPanelController.tutorialEvent = () =>
            {
                sallyPanelController.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        // step = 9 출전 버튼 누르면 RollBack, Next만
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var sallyPanel = panel_list.panel_list[1].parent.GetComponent<SallyPanelController>();
            sallyPanel.tutorialEvent = () =>
            {
                sallyPanel.tutorialEvent = null;
                darkboard.gameObject.SetActive(false);
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
            };
        });

        //step = 10
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                Next();
            });
        });

        //step = 11
        tutorialEvents.Add(() => 
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var reorderableList = panel_list.panel_list[0].childList[0].GetComponent<ReorderableList>().IsDraggable = false;

            var robotBtnList = panel_list.panel_list[1].parent;
            foreach (Transform robotBtn in robotBtnList.transform)
            {
                robotBtn.GetComponent<Button>().enabled = false;
            }

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                Next();
            });
        });

        //step = 12
        tutorialEvents.Add(() => 
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var startBtn = panel_list.panel_list[1].parent.gameObject;
            var selectedAgent = panel_list.panel_list[2].parent.GetComponent<SelectedAgentsPanel>();

            var robotBtnList = panel_list.panel_list[3].parent;
            var reorderableList = panel_list.panel_list[4].parent.GetComponent<ReorderableList>().IsDraggable = true;
            foreach (Transform robotBtn in robotBtnList.transform)
            {
                robotBtn.GetComponent<Button>().enabled = true;
            }

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                startBtn.gameObject.SetActive(false);
                darkboard.gameObject.SetActive(false);

                selectedAgent.tutorialEvent = () =>
                {
                    selectedAgent.tutorialEvent = null;
                    startBtn.gameObject.SetActive(true);
                    darkboard.gameObject.SetActive(true);
                    Next();
                };
            });
        });

        //step = 13
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var startBtn = panel_list.panel_list[1].parent.GetComponent<AgentSelectPanel>();
            startBtn.tutorialEvent = () =>
            {
                startBtn.tutorialEvent = null;
                enemyCompulsionReinforce = 2f;
                darkboard.gameObject.SetActive(false);
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
            };
        });

        //step = 14
        tutorialEvents.Add(tutorialEvents[2]);

        //step = 15
        tutorialEvents.Add(() =>
        {
            enemyCompulsionReinforce = 1f;
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var teamBtn = panel_list.panel_list[0].childList[0].GetComponent<TopMenuButton>();
            teamBtn.tutorialEvent = () =>
            {
                RollBack(Step);
                teamBtn.tutorialEvent = () =>
                {
                    teamBtn.tutorialEvent = null;
                    Next();
                };
            };
        });

        //16
        tutorialEvents.Add(() => {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var hangerBtn = panel_list.panel_list[0].childList[0].GetComponent<SubMenuButton>();
            hangerBtn.tutorialEvent = () =>
            {
                hangerBtn.tutorialEvent = null;
                RollBack(Step);
            };

            var lobbyCanvas = panel_list.panel_list[1].parent.GetComponent<LobbyCanvasManager>();
            lobbyCanvas.tutorialEvent = () =>
            {
                lobbyCanvas.tutorialEvent = null;
                Next();
            };
        });

        //17
        tutorialEvents.Add(tutorialEvents[2]);

        //18
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var equipBtn = panel_list.panel_list[0].childList[0].GetComponent<NextPanel>();
            equipBtn.tutorialEvent = () =>
            {
                equipBtn.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        //19
        tutorialEvents.Add(tutorialEvents[2]);

        //20
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var robotSlotList = panel_list.panel_list[1].parent;
            foreach(Transform robotSlot in robotSlotList.transform)
            {
                robotSlot.GetComponent<Button>().enabled = false;
            }
        });

        //21
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var hangerBtn = panel_list.panel_list[1].parent.GetComponent<HangarPanelController>();
            var teamInfo = panel_list.panel_list[2].parent.GetComponent<TeamAgentInfoPanelController>();
            var robotSlotList = panel_list.panel_list[3].parent;
            foreach (Transform robotSlot in robotSlotList.transform)
            {
                robotSlot.GetComponent<Button>().enabled = true;
            }

            hangerBtn.tutorialEvent = () =>
            {
                hangerBtn.tutorialEvent = null;
                teamInfo.RepairDurability();
                RollBack(Step);
                Next();
            };
        });

        //22
        tutorialEvents.Add(tutorialEvents[2]);

        //23 -> AgentInfoContent showDetailInfo에 추가 로직 있음
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
        });

        //step 24; 코어 부분 선택 강조;
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var coreSlotBtn = panel_list.panel_list[0].childList[0].GetComponent<EquipmentSelectButton>();
            coreSlotBtn.tutorialEvent = () =>
            {
                coreSlotBtn.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        //step 25
        tutorialEvents.Add(tutorialEvents[2]);

        //step 26
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            var equipPanel = panel_list.panel_list[0].parent.GetComponent<EquipmentPanelController>();
            equipPanel.tutorialEvent = () =>
            {
                equipPanel.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        //step 27
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                RollBack(Step);
                Next();
            });
        });

        //step 28
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            //행거 패널에 장비창 닫혔을 때의 이벤트가 있음. 그 이벤트를 백 버튼에 등록해서 사용중.
            var hangerPanel = panel_list.panel_list[1].parent.GetComponent<HangarPanelController>();
            hangerPanel.tutorialEvent = () =>
            {
                hangerPanel.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        //step 29; 무기 부분 선택 강조;
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.8f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var weaponSlotBtn = panel_list.panel_list[0].childList[0].GetComponent<EquipmentSelectButton>();
            weaponSlotBtn.tutorialEvent = () =>
            {

                weaponSlotBtn.tutorialEvent = null;
                RollBack(Step);
                Next();
            };
        });

        //30; 대화창
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            var equipPanel = panel_list.panel_list[0].parent.GetComponent<EquipmentPanelController>();
            int i = 0;
            equipPanel.tutorialEvent = () =>
            {
                if (i >= 1)
                {
                    equipPanel.tutorialEvent = null;
                    RollBack(Step);
                    Next();
                }
                ++i;
            };
        });

        //step 31
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            panel_list.panel_list[0].parent.SetActive(false);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                darkboard.gameObject.SetActive(false);
                RollBack(Step);
            });
        });

        //step 32
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(false);
            textBoardBtn.gameObject.SetActive(false);
        });

        //step 33
        tutorialEvents.Add(() =>
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var teamBtn = panel_list.panel_list[0].childList[0].GetComponent<TopMenuButton>();
            teamBtn.tutorialEvent = () =>
            {
                RollBack(Step);
                teamBtn.tutorialEvent = () =>
                {
                    teamBtn.tutorialEvent = null;
                    Next();
                };
            };
        });

        //step 34
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var aiBtn = panel_list.panel_list[0].childList[0].GetComponent<SubMenuButton>();
            aiBtn.tutorialEvent = () => { RollBack(Step); aiBtn.tutorialEvent = null; };

            var createTab = panel_list.panel_list[1].parent.GetComponent<TabChangeButton>();
            var lobbyPanel = panel_list.panel_list[2].parent.GetComponent<LobbyCanvasManager>();
            createTab.tutorialEvent = () =>
            {
                createTab.tutorialEvent = null;
                createTab.OnClicked();
                lobbyPanel.tutorialEvent = () =>
                {
                    lobbyPanel.tutorialEvent = null;
                    Next();
                };
            };
        });

        //step 35
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var createPanel = panel_list.panel_list[1].parent.GetComponent<CreatedAIInventoryPanelController>();
            createPanel.tutorialEvent = () =>
            {
                createPanel.tutorialEvent = null;
                RollBack(Step);
                Next();
                //Next();
            };
        });

        //step 36
        tutorialEvents.Add(tutorialEvents[2]);

        //step 37; 보상 설정 체크 박스 모두 선택이 끝날 경우
        tutorialEvents.Add(() => {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var createPanel = panel_list.panel_list[1].parent.GetComponent<AICreatePanelController>();
            createPanel.tutorialEvnet = () =>
            {
                createPanel.tutorialEvnet = null;
                RollBack(Step);
                Next();
            };
        });

        //step 38
        tutorialEvents.Add(tutorialEvents[2]);

        //step 39
        tutorialEvents.Add(() =>
        {
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            var startBtn = panel_list.panel_list[1].parent.GetComponent<AICreatePanelController>();
            startBtn.tutorialEvnet = () =>
            {
                startBtn.tutorialEvnet = null;
                darkboard.color = Color.clear;
                RollBack(Step);
            };
        });

        //step 40
        tutorialEvents.Add(() =>
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);
            panel_list.panel_list[0].parent.gameObject.SetActive(false);
            var aiCreateDataRecorder = panel_list.panel_list[1].parent.GetComponent<AICreateDataRecorder>();
            aiCreateDataRecorder.tutorialEvent = () =>
            {
                aiCreateDataRecorder.tutorialEvent = null;
                panel_list.panel_list[0].parent.gameObject.SetActive(true);
                RollBack(Step);
                Next();
            };
        });

        //Step 41
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                darkboard.gameObject.SetActive(false);
                RollBack(Step);
            });
        });

        //Step 42
        tutorialEvents.Add(() => 
        {
            darkboard.gameObject.SetActive(true);
            darkboard.color = Color.clear;
            darkboard.DOColor(Color.black * 0.5f, darkTweenTime);
            SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list);

            textBoardBtn.interactable = true;
            textBoardBtn.onClick.AddListener(() =>
            {
                textBoardBtn.interactable = false;
                textBoardBtn.onClick.RemoveAllListeners();
                textBoardBtn.gameObject.SetActive(false);
                LobbyCanvasManager.instance.backButton.RemoveEventOnButton();
                LobbyCanvasManager.instance.backButton.AddEventOnButton();
                LobbyCanvasManager.instance.ChangeMenu(LobbyCanvasManager.MenuState.Main);
                RollBack(Step);
                Next();
            });
        });

        //Step 43
        tutorialEvents.Add(tutorialEvents[2]);

        //삭제하기 전 저장
        tutorialEvents.Add(() =>
        {
            instance.gameObject.SetActive(false);
            RBMSaveManager.instance.SaveAllData();
            endEvent = () => 
            {
                endEvent = null;
                Next();
            };
        });

        //삭제
        tutorialEvents.Add(() =>
        {
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
            instance = null;
        });

    }

    public void RollBack(int orderStep)
    {
        if (panelListDB.TryGetValue(orderStep, out TutorialPanelList beforePanels))
        {
            foreach (var panelList in beforePanels.panel_list)
            {
                var i = 0;
                foreach (var child in panelList.childList)
                {
                    (child.transform as RectTransform).SetParent(panelList.parent.transform);
                    child.transform.SetSiblingIndex(panelList.siblingIndexList[i]);
                    i++;
                }
            }
        }
    }

    private void SettingContentAndPanel(out TutorialTextBoard content, out TutorialPanelList panel_list)
    {
        if (contentDB.TryGetValue(step, out content))
        {
            textBoardTr.gameObject.SetActive(true);
            textBoardTr.offsetMin = new Vector2(0f, 1f);
            textBoardTr.offsetMax = new Vector2(0f, 0f);
            textBoardTr.anchorMin = content.anchors_min;
            textBoardTr.anchorMax = content.anchors_max;
            tutorialText.text = content.content;
        }
        else textBoardTr.gameObject.SetActive(false);

        if (panelListDB.TryGetValue(step, out panel_list))
        {
            foreach (var panelList in panel_list.panel_list)
            {
                foreach (var child in panelList.childList)
                {
                    (child.transform as RectTransform).SetParent(transform);
                }
            }
        }
        textBoardBtn.transform.SetAsLastSibling();
    }


    public void Next()
    {
        ++step;
        tutorialEvents[step]();




        // 아래 로직 모두 tutorailEvents로 옮김
        //if (contentDB.TryGetValue(step, out TutorialTextBoard valContent))
        //{
        //    textBoardTr.gameObject.SetActive(true);
        //    textBoardTr.position = valContent.pos;
        //    textBoardTr.sizeDelta = valContent.size;
        //    tutorialText.text = valContent.content;
        //}
        //else textBoardTr.gameObject.SetActive(false);

        //if(panelDB.TryGetValue(step, out TutorialPanel valPanel))
        //{
        //    foreach(var child in valPanel.childList)
        //        (child.transform as RectTransform).SetParent(transform);
        //}
    }




    // <튜토리얼 메모장. 로직적인 사용 목적은 아님.>
    [System.Serializable]
    public class TutorialTooltip
    {
        public int step;
        [TextArea] public string tooltip;
    }

    // <튜토리얼의 쓰일 대화창. 로직적인 사용 목적. 대화창을 하나만 쓰기 위한 필요 데이터를 포함>
    [System.Serializable]
    public class TutorialTextBoard
    {
        public int step;
        public Vector2 anchors_min;
        public Vector2 anchors_max;
        [TextArea] public string content;
    }

    // <튜토리얼의 쓰일 패널 or 버튼. 로직적인 사용 목적. 패널의 우선 순위(z order) 변경 등이 이뤄짐.>
    [System.Serializable]
    public class TutorialPanel
    {
        public GameObject parent;
        public List<GameObject> childList;
        [HideInInspector] public List<int> siblingIndexList;
    }

    [System.Serializable]
    public class TutorialPanelList
    {
        public int step;
        public List<TutorialPanel> panel_list;
    }
}
