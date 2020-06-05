using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XftWeapon;
using UnityEngine.UI;
using MEC;


using static MiniLegend.MyGamePadMapping;
using static MiniLegend.MyStorageItems;
using MiniLegend.CloudServices;
using UnityEngine.AI;

namespace MiniLegend.CharacterStats
{
    /// <summary>
    /// Class use to control mainCharacter's movement and behavior baseon user input 
    /// </summary>
    public class MyCharacterController : MyCharacterStats
    {
        protected internal static int knockDownWeaponId = 0; 
        public enum HealerSpell { Heal, AttackBuff, DefenseBuff, Regen };
        public const int DESKTOP_DEVICE = 3;
        public static bool frezeeMovement = false;
        public static bool isEscaping = false;
        public float speed = 1.0f;
        public float turnSpeed = .1f;
        public float jumpSpeed = 8.0f;
        public float gravity = 20.0f;
        public float waitTimeinSeconds = .3f;
        public float backwardSpeed = 2f;

        public AudioClip powerUpFX;
        public AudioClip shieldFX;
        public AudioClip regenFX;
        public AudioClip healFX;
        public GameObject cameraObject;
        public GameObject healerTargetMarkParticle;
        public GameObject crosshairCamera;

    
        public GameObject crosshair;
        public GameObject healerTarget = null;
        public ParticleSystem weaponParticle;

        public MyAudioSource attackVoiceSource;
        public MyAudioSource spinningAudioSource;

        public MyVillagerController myVillagerController;
        public HealerSpell healerSpell;
   
        public List<Item> myItemsInventory = new List<Item>();

        public bool proformAttack = false;
        private bool shownDamageOverlay = false;
        private float defaultSpeed = 1f;
        private float velX = 0f;
        private float velY = 0f;
        private float velX2 = 0f;
        private float velY2 = 0f;
        private float angle = 0f;
        private float dPadX = 0f;
        private float dPadY = 0f;
        private float deadZone = .05f;
        private int lastColliderID = 0;
        
        private float leftTrigger = 0f;
        private float rightTrigger = 0f;

        private bool isDead = false;
        private bool dPadXPressed = false;
        private bool dPadYPressed = false;
        private bool itemUsed = false;
    
        private int runState = Animator.StringToHash("Base Layer.Run");
        private int attackState = Animator.StringToHash("Attack Layer.Attack1");
        private int attack2State = Animator.StringToHash("Attack Layer.Attack2");
        private int attack3State = Animator.StringToHash("Attack Layer.Attack3");
        private int attack4State = Animator.StringToHash("Attack Layer.Attack4");
        private int comboState = Animator.StringToHash("Base Layer.Combo1");

        private int attackToggleState = Animator.StringToHash("Base Layer.Attack1Toggle");
        private int healingState = Animator.StringToHash("Base Layer.spellLoop");
        private int idleState = Animator.StringToHash("Base Layer.Idle");
        private int spellEndState = Animator.StringToHash("Base Layer.SpellEnd");
        private int tempHp = 0;

        private AnimatorStateInfo currentBaseLayerStateInfo;
        private AnimatorStateInfo currentAttackLayerStateInfo;
        private Vector3 moveDirection = Vector3.zero;
   
        private Vector3 velocity = Vector3.zero;
        private Vector3 startPosition = Vector3.zero;
        private Quaternion startRotation = Quaternion.identity;
        private Vector3 particleOffset = new Vector3(0, 1, 0);
        private Vector3 targetParticleOffset = new Vector3(0, 3, 0);
       
        private bool fireButtonPressOnce = false;
        private bool fireButtonPressTwice = false;
        private bool fireButtonPressThree = false;
        private float fireButtonPressTime;
        private Vector3 playerDirection = Vector3.zero;
        private Vector3 crossHairDirection = Vector3.zero;
        private Vector3 xRotationAmount = new Vector3(20, 0, 0);
        private Vector3 yRotationAmount = new Vector3(0, 90, 0);
        private Vector3 rayCastPosition = Vector3.zero;

        private bool escapePressed = false;
        private bool setAttackAnimation = false;
        private bool isButtonPressed = false;

        private bool useItem = false;
        private bool isHealing = false;
        private float lastHealTime = 0f;
        private float lastAttackVoice = 0f;
        private static int MAXLEVEL = 25;
        private Color defaultOutlineColor;
        private Color highLightOutlineColor;
        private float comboTriggerTimeLimit = .3f;
        private int deviceType = 0;
        private bool returnToMainMenu = false;
        private GameObject onHitPartice;
        private int index = 0;
        private int healerTargetIndex = 0;
        private MyCharacterStats myCharacterStatsCached;
        private MyMonsterNavMeshAgent myMonsterNavMeshAgentCached;
        private float damgageOverlayStartTime = 0;

        private NavMeshHit navMeshHit;
        private Vector3 rayCastOffset = new Vector3(0, 1, 0);
        private int qualityLevel = 0;
        private ItemContainer itemContainer = null;
        
        private Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);

        private TimeSpan attackTimeDiff;
        private TimeSpan defenseTimeDiff;
      

        // Use this for initialization
        protected internal void Start()
        {
            Init();
        }

        protected override void Init()
        {
            if (isInitialized)
                return;

            playerStats = new MyPlayerStats[26];
            CalculateStats();
            base.Init();
           
            //Desktop =3 Console = 2
            deviceType = (int)SystemInfo.deviceType;
            if (myVillagerController == null)
            {
                myVillagerController = MyStaticGameObjects.instance.arenaNPC.GetComponent<MyVillagerController>();
            }

            returnToMainMenu = false;     
            myWeapons = currentWeapon.GetComponent<MyWeapons>();
            myWeaponCollider = currentWeapon.GetComponent<Collider>();
            defaultSpeed = speed;
            weaponType = myWeapons.itemType;
            SwitchWeapon(myWeapons.weaponId);
            InitWeaponTrail();
            StopWeaponTrail();
            startPosition = theTransform.position;
            startRotation = theTransform.rotation;
            playerDirection = crossHairDirection = theTransform.rotation.eulerAngles;
          
            if (null == animator)
            {
                animator = GetComponent<Animator>();
            }
            if (null == rigidbody)
            {
                rigidbody = GetComponent<Rigidbody>();
            }

         
            //if (isHealer)
            SwitchHealerTarget(gameObject);


            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                comboTriggerTimeLimit = .8f;
            }
            LoadInventory();

            if (MyStaticGameObjects.qualityLevel > 2)
            {
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }

            ResetBoostStack();
            TurnOffWeaponLight();

            //  Debug.Log("DeviceType: " + deviceType);
            if (MyStaticGameObjects.instance.debugOn)
            {
                level = 0;
                for (int x = 0; x < MyStaticGameObjects.debugLevel; x++)
                {
                    LevelUp();
               
                }
            }
            else
            {
                level = 0;
                LevelUp();
            }
            if (knockDownArea != null)
            {
                knockDownTransform = knockDownArea.transform;
                knockDownCollider = knockDownArea.GetComponent<Collider>();
                knockDownCollider.enabled = false;
                
                knockDownWeaponId = knockDownArea.GetInstanceID();
                knockDownArea.transform.SetParent(null);
                knockDownArea.SetActive(true);
            }
        }

        /// <summary>
        /// Restore the player rotation after the inital cut scene 
        /// </summary>
        protected internal void FixPlayerRotation()
        {
            playerDirection = crossHairDirection = theTransform.rotation.eulerAngles;
        }

        /// <summary>
        /// Show a damage overlay for .3 seconds
        /// </summary>
        protected internal override void OneSecondInterval()
        {
            base.OneSecondInterval();
            if(shownDamageOverlay&&  Time.time - damgageOverlayStartTime> .3f)
            {
                shownDamageOverlay = false;
                MyStaticGameObjects.instance.damageOverlay.enabled = false;
            }
        }

        /// <summary>
        /// Show the crosshair for the archer
        /// </summary>
        protected internal void ShowCrosshair()
        {
            if (crosshair != null)
            {
                crosshair.SetActive(true);
            }
            if (crosshairCamera != null)
            {
                crosshairCamera.SetActive(true);
            }
        }

        /// <summary>
        /// Reset escape button after one second
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> ResetEscapeButton()
        {
            yield return Timing.WaitForSeconds(1f);
            escapePressed = false;
        }

        /// <summary>
        /// Controls player movement, attack and death base on the user input
        /// </summary>
        // Update is called once per frame
        void Update()
        {
          
            //Game Over return to main menu
            if (!myVillagerController.isSurviorMode && MyMonsterSpawner.MyMonsterSpawnerInstance.hasGameEnded && !MyMonsterSpawner.MyMonsterSpawnerInstance.retryScreenOpen && MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.CONFIRMBUTTON))
            {
                if (!returnToMainMenu)
                {
                    returnToMainMenu = true;
                    //Timing.RunCoroutine(MyStaticGameObjects.instance.ReturnToMainMenu());
                    MyStaticGameObjects.instance.ReturnToMainMenu();
                }
            }

        

            if (hp > 0)
            {
                //player revived
                if (isDead)
                {
                    resetingTimeout = false;
                    animator.SetBool("Die", false);
                    isDead = false;
                    //StartBlobShadowHeightCalculation();
                }

                OneSecondInterval();
                CalculateBlogShadowHeight();
                //Menu and Overlay BEGIN
                if (!MyStaticGameObjects.instance.foundItemOverlay.activeSelf)
                {
                    if (!MyStaticGameObjects.instance.menuPanel.activeSelf && !MyStaticGameObjects.instance.dialogWindow.activeSelf)
                    {
                        if (!MyStaticGameObjects.instance.iAPManager.gemStoreWindowPanel.activeSelf && !MyStaticGameObjects.instance.iAPManager.itemWindowPanel.activeSelf)
                        {
                            //Button Map
                            if (!MyStaticGameObjects.instance.buttonMap.activeSelf && MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.LEFTB))
                            {
                                MyStaticGameObjects.instance.buttonMap.SetActive(true);
                                Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.dialogOpenFx));
                                MyStaticGameObjects.UnlockCursor();
                            }
                            else if (MyStaticGameObjects.instance.buttonMap.activeSelf &&  (deviceType == DESKTOP_DEVICE && Input.GetMouseButtonUp(1) || MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.LEFTB)|| MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.CANCELBUTTON)))
                            {
                                MyStaticGameObjects.instance.buttonMap.SetActive(false);
                                if (MyStaticGameObjects.instance.isGamePaused)
                                {
                                    MyStaticGameObjects.instance.PauseGame(false);
                                }
                                if (MyCrossFadeAudioMixer.instance != null)
                                {
                                    Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.dialogCloseFx));
                                }
                                MyStaticGameObjects.LockCursor();
                               // Debug.Log("Closing the Button Layout Menu");

                            }

                            if (!MyStaticGameObjects.instance.buttonMap.activeSelf)
                            {
                                if (MyStorageController.currentStorage != null && ((deviceType == DESKTOP_DEVICE && Input.GetMouseButtonUp(0)) || MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.USEBUTTON)))
                                {
                                    //Open Chest 

                                    MyStorageController.currentStorage.GetComponent<MyStorageController>().Open();

                                    if (MyStorageController.currentStorage.GetComponent<MyStorageController>().isOpened && MyStorageController.currentStorage.GetComponent<MyStorageController>().items.Count > 0)
                                    {

                                        AddItems(MyStorageController.currentStorage.GetComponent<MyStorageController>().items);
                                        // myItemsInventory.AddRange(MyStorageController.currentStorage.GetComponent<MyStorageController>().items);
                                        MyStorageController.currentStorage.GetComponent<MyStorageController>().ClearItems();
                                        if (MyMainQuestEndingTrigger.instance.CheckQuestCondidition())
                                        {
                                            MyStaticGameObjects.instance.mainQuestText.GetComponent<Text>().text = "Find and defeat King Snow";

                                        }
                                    }
                                }
                                //Menu Panel
                                else if (MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.MENUBUTTON))
                                {
                                    if (!MyStaticGameObjects.instance.isGamePaused)
                                    {
                                        MyStaticGameObjects.instance.PauseGame();
                                    }
                                    MyStaticGameObjects.instance.menuPanel.SetActive(true);
                                    MyStaticGameObjects.UnlockCursor();
                                    Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.selectFx));
                                }
                                else if (!MyStaticGameObjects.instance.isGamePaused && MyVillagerController.nextTothisNPC != null && (MyGamePadMapping.MyGamePadMappingInstance.GetButtonUp(ButtonMap.USEBUTTON)||(deviceType == DESKTOP_DEVICE && Input.GetMouseButtonDown(0))))
                                {
                                    //talk to NPC
                                    MyVillagerController.nextTothisNPC.GetComponent<MyVillagerController>().ProformNPCAction();
                                }
                            }

                        }

                        if (!MyStaticGameObjects.instance.buttonMap.activeSelf && MyGamePadMappingInstance.GetButtonUp(ButtonMap.PAUSEBUTTON))
                        {
                            // Store

                            if (!MyStaticGameObjects.instance.iAPManager.gameObject.activeSelf)
                            {
                                MyStaticGameObjects.instance.iAPManager.ToggleItemStore(true);

                                Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.selectFx));
                            }
                            else
                            {
                                MyStaticGameObjects.instance.iAPManager.ToggleItemStore(false);

                                Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.dialogCloseFx));
                            }
                        }
                    }
                }
                //Menu and Overlay ENDS

                //ACTION BEGIN 
                //Arena Exit 
                if (MyVillagerController.playerIsInArena) {
                    if ((MyGamePadMappingInstance.GetCurrentInputType() == InputType.GAMECONTROLLER &&  MyGamePadMappingInstance.GetAxis(ButtonMap.LEFTTRIGGER) > 0 && MyGamePadMappingInstance.GetAxis(ButtonMap.RIGHTTRIGGER) > 0)
                        || (MyGamePadMappingInstance.GetCurrentInputType() == InputType.KEYBOARDANDMOUSE && Input.GetKeyUp(KeyCode.Escape))
                        || (MyGamePadMappingInstance.GetCurrentInputType() == InputType.TOUCHSCREEN && MyGamePadMappingInstance.GetButtonUp(ButtonMap.ESCAPEBUTTON))
                        )
                    {
                        if (MyStaticGameObjects.instance.arenaNPC != null)
                        {
                            MyStaticGameObjects.instance.arenaNPC.GetComponent<MyVillagerController>().ExitArena();

                        }
                        //ExitArena();
                    }
                }
                //Escaping
                if (MyGamePadMappingInstance.GetButtonDown(ButtonMap.ESCAPEBUTTON))
                {
                    if (hp > 0)
                    {
                        isEscaping = true;
                        //Debug.Log("escaping"); 
                    }
                }
                else if (MyGamePadMappingInstance.GetButtonUp(ButtonMap.ESCAPEBUTTON))
                {
                    isEscaping = false;
                }


                if (!frezeeMovement && !MyStaticGameObjects.instance.isGamePaused)
                {
                    //Attack animations
                    if (MyStorageController.currentStorage == null && MyVillagerController.nextTothisNPC == null)
                    {
                        if (MyGamePadMapping.MyGamePadMappingInstance.GetButtonDown(ButtonMap.ATTACK2BUTTON))
                        {
                            arrowType = MyCompanionRangeWeapon.ArrowType.Magic;
                            MyStaticGameObjects.attackProform++;
                            proformAttack = true;

                            if (isHealer)
                            {
                                animator.SetBool("Attack1", true);
                                healerSpell = HealerSpell.AttackBuff;
                            }
                            else
                            {
                                animator.SetTrigger("Attack2Toggle");

                            }
                        }
                        if ( (deviceType == DESKTOP_DEVICE && Input.GetMouseButtonDown(1)) || MyGamePadMapping.MyGamePadMappingInstance.GetButtonDown(ButtonMap.ATTACK3BUTTON))
                        {
                            MyStaticGameObjects.attackProform++;
                            proformAttack = true;
                            arrowType = MyCompanionRangeWeapon.ArrowType.Fire;

                            if (isHealer)
                            {
                                animator.SetBool("Attack1", true);
                                healerSpell = HealerSpell.Regen;
                            }
                            else
                            {
                                animator.SetTrigger("Attack3Toggle");
                            }
                        }
                        if (MyGamePadMapping.MyGamePadMappingInstance.GetButtonDown(ButtonMap.JUMPBUTTON))
                        {
                            MyStaticGameObjects.attackProform++;
                            proformAttack = true;
                            arrowType = MyCompanionRangeWeapon.ArrowType.Poison;

                            if (isHealer)
                            {
                                animator.SetBool("Attack1", true);
                                healerSpell = HealerSpell.DefenseBuff;
                            }
                            else
                            {
                                animator.SetTrigger("Attack4Toggle");
                            }
                        }
                
                        //fireButtonPressTime - Time.time < -1
                        if (((deviceType == DESKTOP_DEVICE && Input.GetMouseButtonDown(0)) ||MyGamePadMappingInstance.GetButtonDown(ButtonMap.ATTACK1BUTTON)) && !isButtonPressed && !IsInAnimation(comboState))
                        {
                           
                            arrowType = MyCompanionRangeWeapon.ArrowType.Lightning;

                            //Combo Pressed 1 time
                            isButtonPressed = true;
                            //Debug.Log(Time.time -fireButtonPressTime );

                            if (isHealer)
                            {
                                animator.SetBool("Attack1", true);
                                healerSpell = HealerSpell.Heal;
                            }
                            else if (isArcher)
                            {
                                MyStaticGameObjects.attackProform++;
                                proformAttack = true;
                                animator.SetTrigger("Attack1Toggle");

                            }
                            else
                            {
                                if (fireButtonPressOnce && !fireButtonPressTwice && Time.time - fireButtonPressTime < comboTriggerTimeLimit && velY == 0 && velX == 0 && !animator.GetBool("InCombo"))
                                {
                                    //Combo Pressed 2 time
                                    fireButtonPressTwice = true;
                                }
                                else if (fireButtonPressTwice && Time.time - fireButtonPressTime < .3)
                                {
                                    //Combo Pressed 3 times Trigger a combo
                                    MyStaticGameObjects.attackProform++;
                                    proformAttack = true;
                                    animator.SetBool("InCombo", true);
                                    animator.SetTrigger("Combo1");
                                    fireButtonPressOnce = false;
                                    fireButtonPressTwice = false;
                                }
                                else if (!setAttackAnimation && Time.time - fireButtonPressTime > .3)
                                {
                                    MyStaticGameObjects.attackProform++;
                                    proformAttack = true;
                                    fireButtonPressOnce = false;
                                    fireButtonPressTwice = false;
                                    fireButtonPressOnce = true;

                                    setAttackAnimation = true;
                                    animator.SetTrigger("Attack1Toggle");
                                }
                                else if (!IsInAnimation(comboState))
                                {
                                    MyStaticGameObjects.attackProform++;
                                    proformAttack = true;
                                    setAttackAnimation = true;
                                    animator.SetTrigger("Attack1Toggle");
                                }
                            }

                            fireButtonPressTime = Time.time;
                            if (Time.time - fireButtonPressTime > .3)
                            {
                                //reset combo button counts
                                fireButtonPressOnce = false;
                                fireButtonPressTwice = false;

                            }

                        }
                        else if (MyGamePadMappingInstance.GetButtonUp(ButtonMap.ATTACK1BUTTON) || (deviceType == DESKTOP_DEVICE && Input.GetMouseButtonUp(0)))
                        {
                           
                            animator.SetBool("Attack1", false);
                            setAttackAnimation = false;
                            isHealing = false;
                           
                            isButtonPressed = false;
                        }
                        else if (MyGamePadMappingInstance.GetButtonUp(ButtonMap.ATTACK2BUTTON)
                            || (deviceType == DESKTOP_DEVICE && Input.GetMouseButtonUp(1))
                            || MyGamePadMappingInstance.GetButtonUp(ButtonMap.ATTACK3BUTTON)
                            || MyGamePadMappingInstance.GetButtonUp(ButtonMap.JUMPBUTTON))
             
                        {
                            if (isHealer)
                                animator.SetBool("Attack1", false);
                        }
                        dPadX = MyGamePadMapping.MyGamePadMappingInstance.GetAxis( ButtonMap.DPADXAXIS);
                        dPadY = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.DPADYAXIS); 
                        leftTrigger = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.LEFTTRIGGER);
                        rightTrigger = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.RIGHTTRIGGER);


                        //Use potion
                        if (leftTrigger> 0 && !itemUsed)
                        {
                            UsePotion();
                        }

                        //Use reveive potion
                        if (rightTrigger > 0 && !itemUsed)
                        {
                            UseRevivePotion();
                        }
                        if (leftTrigger <= 0 && rightTrigger <= 0)
                        {
                            itemUsed = false;
                        }
                      
                        if (rightTrigger <= 0 && leftTrigger <= 0)
                        {
                            if (dPadX > 0 && !dPadXPressed)
                            {
                                if(healerTarget == gameObject)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion1);
                                }
                                else if(healerTarget == MyStaticGameObjects.instance.companion1)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion2);
                                }
                                else if (healerTarget == MyStaticGameObjects.instance.companion2)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion3);
                                }
                                else if (healerTarget == MyStaticGameObjects.instance.companion3)
                                {
                                    SwitchHealerTarget(gameObject);
                                }
                                   
                                dPadXPressed = true;
                            }else if (dPadX < 0 && !dPadXPressed )
                            {
                                if (healerTarget == gameObject)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion3);
                                }
                                else if (healerTarget == MyStaticGameObjects.instance.companion1)
                                {
                                    SwitchHealerTarget(gameObject);
                                }
                                else if (healerTarget == MyStaticGameObjects.instance.companion2)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion1);
                                }
                                else if (healerTarget == MyStaticGameObjects.instance.companion3)
                                {
                                    SwitchHealerTarget(MyStaticGameObjects.instance.companion2);
                                }
                                dPadXPressed = true;
                            }
                        }
                      

                        if (dPadX == 0)
                        {
                            dPadXPressed = false;
                        }
                      
                       
                    }
                    //Debug code 
                    if (MyStaticGameObjects.instance.debugOn && Input.GetKeyUp(KeyCode.F12))
                    {

                        if (MyStaticGameObjects.instance.myReportCard.gameObject.activeSelf)
                        {
                            MyStaticGameObjects.instance.myReportCard.gameObject.SetActive(false);
                        }
                        else
                        {
                            MyStaticGameObjects.ShowReportCard();
                        }
                    }
               

                    velX = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.XAXIS);
                    velY = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.YAXIS);
                    if (MyStaticGameObjects.cursorLocked || MyGamePadMapping.MyGamePadMappingInstance.GetCurrentInputType() == InputType.TOUCHSCREEN)
                    {
                        velX2 = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.XAXIS2);
                        velY2 = MyGamePadMapping.MyGamePadMappingInstance.GetAxis(ButtonMap.YAXIS2);
                    }
     
                    if (velY == 0 && velX == 0)
                    {
                        ResetRunAnimation();
                  
                    }
                    else
                    {
                    
                        SetRunAnimation();
                
                    }

     
                    moveDirection.x = 0;
                    moveDirection.y = 0;
                    moveDirection.z = 0;  
                  

                    if (velY > 0 || velY < 0)
                    {
                        moveDirection = moveDirection +  Vector3.forward * velY * Time.deltaTime * speed;
                    }
                

                    if (velX > 0 || velX < 0)
                    {
                        moveDirection = moveDirection +  Vector3.right * velX * Time.deltaTime * speed;
                    }
                   

                    if (velX2 > 0)
                    {
                        playerDirection = playerDirection + yRotationAmount * Time.deltaTime * velX2;
                    }
                    else if (velX2 < 0)
                    {
                        playerDirection = playerDirection + yRotationAmount * Time.deltaTime * velX2;
                    }

                    if (playerDirection.sqrMagnitude > 0.0f)
                    {
                        theTransform.rotation = Quaternion.Euler(playerDirection);
                    }

                    if (crosshair != null)
                    {
                        if (velX2 > 0)
                        {
                            crossHairDirection = crossHairDirection + yRotationAmount * Time.deltaTime * velX2;
                        }
                        else if (velX2 < 0)
                        {
                            crossHairDirection = crossHairDirection + yRotationAmount * Time.deltaTime * velX2;
                        }

                        if (velY2 > 0)
                        {
                            crossHairDirection = crossHairDirection + xRotationAmount * Time.deltaTime * velY2;
                        }
                        else if (velY2 < 0)
                        {
                            crossHairDirection = crossHairDirection + xRotationAmount * Time.deltaTime * velY2;
                        }

                        if (crossHairDirection.sqrMagnitude > 0.0f)
                        {

                            crossHairDirection.x = Mathf.Clamp(crossHairDirection.x, -10, 20);
                            crosshair.transform.rotation = Quaternion.Euler(crossHairDirection);
                        }

                    }

                    theTransform.Translate(moveDirection);      

                }
                //ACTION END

            }
            else
            {
                ResetBuffStats();
                //hp is < 1
                isEscaping = false;
                animator.SetBool("Die", true);
                ResetRunAnimation();
                ResetAttackAnimation();

                isDead = true;
        
                if (!isDead)
                {
                    if (NavMesh.SamplePosition(theTransform.position + rayCastOffset,out navMeshHit, 2, NavMesh.AllAreas))
                    {
                     
                        theTransform.position = navMeshHit.position;
                        theTransform.rotation = Quaternion.FromToRotation(Vector3.up, navMeshHit.normal);
                    }
                  
                   
                    isDead = true;
                }

                if (!resetingTimeout)
                {
                    resetingTimeout = true;
                    Timing.RunCoroutine(ResetDeathTimeOut());
                }
              

            }
        }

        /// <summary>
        /// Stop Attack Animation 
        /// </summary>
        public void ResetAttackAnimation()
        {
            animator.SetBool("Attack1", false);
            if (isHealer || isArcher)
            {
                animator.SetBool("Attack2", false);
            }
            if (!isHealer)
            {
                animator.ResetTrigger("Attack1Toggle");
                animator.ResetTrigger("Attack2Toggle");
                animator.ResetTrigger("Attack3Toggle");
                animator.ResetTrigger("Attack4Toggle");
                animator.ResetTrigger("Combo1");
                animator.SetBool("InCombo", false);
            }
        }

        /// <summary>
        /// Switch the Healer Target when the player touch the avatar or user input 
        /// </summary>
        /// <param name="theHealerTarget"></param>
        public void SwitchHealerTarget(GameObject theHealerTarget)
        {
            healerTargetIndex = 0;
            if (healerTarget != null)
            {
                if (healerTarget.GetComponent<MyCharacterStats>().myCharacterPanel != null)
                {
                    healerTarget.GetComponent<MyCharacterStats>().myCharacterPanel.HideTargetArrow();
                }
                
            }
            healerTarget = theHealerTarget;
            if (healerTarget.GetComponent<MyCharacterStats>().myCharacterPanel != null)
            {
                healerTarget.GetComponent<MyCharacterStats>().myCharacterPanel.ShowTargetArrow();
            }
         
        }

        /// <summary>
        /// Start Run animations 
        /// </summary>
        void SetRunAnimation()
        {
            //set run animation
            ResetRunAnimation();
            if (velY > 0)
            {
                //Forward 
                if (velX > 0)
                {
                    animator.SetBool("RunForwardRight", true);
                }
                else if (velX < 0)
                {
                    animator.SetBool("RunForwardLeft", true);
                }
                else
                {
                    animator.SetBool("RunForward", true);
                }
            }
            else if (velY < 0)
            {
                //Backward
                if (velX > 0)
                {
                    animator.SetBool("RunBackwardRight", true);
                }
                else if (velX < 0)
                {
                    animator.SetBool("RunBackwardLeft", true);
                }
                else
                {
                    animator.SetBool("RunBackward", true);
                }

            }
            else if (velX < 0)
            {
                //Left
                animator.SetBool("RunLeft", true);
            }
            else if (velX > 0)
            {
                //Right

                animator.SetBool("RunRight", true);
            }
        }

        /// <summary>
        /// Reset Run animation 
        /// </summary>
        public void ResetRunAnimation()
        {
            animator.SetBool("RunForward", false);
            animator.SetBool("RunForwardRight", false);
            animator.SetBool("RunForwardLeft", false);
            animator.SetBool("RunRight", false);
            animator.SetBool("RunLeft", false);
            animator.SetBool("RunBackward", false);
            animator.SetBool("RunBackwardRight", false);
            animator.SetBool("RunBackwardLeft", false);
        }
   
        /// <summary>
        /// Handler for monster weapon hit the player
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision)
        {
       
            index = 0;
            while (index < collision.contacts.Length)
            {
                if (lastColliderID == collision.contacts[index].otherCollider.gameObject.GetInstanceID())
                {
                    break;
                }
                lastColliderID = collision.contacts[index].otherCollider.gameObject.GetInstanceID();
             
                if (collision.contacts[index].otherCollider.gameObject.layer == MyStaticGameObjects.monsterWeaponLayer)
                {
                    if (!MyStaticGameObjects.instance.hasGameStarted)
                    {

                        particle = MyObjectPools.onHitParticlePoolTransform.GetChild(MyObjectPools.instance.onHitParticlePool.GetMyObjectIndex()).gameObject;
                        particle.transform.position = collision.contacts[index].point + particleOffset;
                        particle.GetComponent<ParticleSystem>().Play();
                        onHitAudioSource.Play();
                        break;
                    }

                  
                    myMonsterNavMeshAgentCached = collision.contacts[index].otherCollider.GetComponent<MyMonsterWeapon>().attacker.GetComponent<MyMonsterNavMeshAgent>();
                    if (myMonsterNavMeshAgentCached.IsWeaponAttacking())
                    {
                        TakesDamage(myMonsterNavMeshAgentCached.GetAttackPoint());
                        onHitAudioSource.Play();

                        if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Werewolf)
                        {
                            onHitPartice = MyObjectPools.wolfAttackParticlePoolTransform.GetChild(MyObjectPools.instance.wolfAttackParticlePool.GetMyObjectIndex()).gameObject;
                            onHitPartice.transform.position = collision.contacts[index].point + particleOffset;
                            onHitPartice.GetComponent<ParticleSystem>().Play();
                        }
                        else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Bandit)
                        {
                            onHitPartice = MyObjectPools.banditAttackParticlePoolTransform.GetChild(MyObjectPools.instance.banditAttackParticlePool.GetMyObjectIndex()).gameObject;
                            onHitPartice.transform.position = collision.contacts[index].point + particleOffset;
                            onHitPartice.GetComponent<ParticleSystem>().Play();
                        }
                        else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Assassin)
                        {
                            onHitPartice = MyObjectPools.assassinAttackParticlePoolTransform.GetChild(MyObjectPools.instance.assassinAttackParticlePool.GetMyObjectIndex()).gameObject;
                            onHitPartice.transform.position = collision.contacts[index].point + particleOffset;
                            onHitPartice.GetComponent<ParticleSystem>().Play();
                        }
                        else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.DarkKnight)
                        {
                            onHitPartice = MyObjectPools.darkknightAttackParticlePoolTransform.GetChild(MyObjectPools.instance.darkknightAttackParticlePool.GetMyObjectIndex()).gameObject;
                            onHitPartice.transform.position = collision.contacts[index].point + particleOffset;
                            onHitPartice.GetComponent<ParticleSystem>().Play();
                        }
                    }


                }
                else if (collision.contacts[index].otherCollider.gameObject.layer == MyStaticGameObjects.monsterWeaponRangeLayer)
                {
                    TakesDamage(collision.contacts[index].otherCollider.GetComponent<MyMonsterRangeWeapon>().attackPoint);
                    onHitAudioSource.Play();
                }
                index++;

            }
            lastColliderID = 0;
        }

        /// <summary>
        /// Use a hp potion
        /// </summary>
        public void UsePotion()
        {
            myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();
            if (myCharacterStatsCached.GetHp() > 0)
            {
                tempHp = myCharacterStatsCached.GetMaxHP();
                if (myCharacterStatsCached.GetHp() < tempHp && UseItem(ItemsId.POTION))
                {
                    myCharacterStatsCached.GiveHealth(100);
                    myCharacterStatsCached.UpdateHealthBar();
                    itemUsed = true;
                }
            }
        }

        /// <summary>
        /// Use a revive potion
        /// </summary>
        public void UseRevivePotion()
        {
            myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();

            if (myCharacterStatsCached.GetHp() < 1 && UseItem(ItemsId.REVIVEPOTION))
            {
                myCharacterStatsCached.GiveHealth(50);
                myCharacterStatsCached.UpdateHealthBar();
                itemUsed = true;
            }
            
        }

        /// <summary>
        /// Use an Item in the inventory
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool UseItem(ItemsId itemId)
        {
            int n = 0;
            useItem = false;
            while (n < myItemsInventory.Count)
            {
                if (myItemsInventory[n].itemId == itemId)
                {
                 
                    // Item item = myItemsInventory[n];
                    if (myItemsInventory[n].amount > 0)
                    {
                        useItem = true;
                        myItemsInventory[n].amount = myItemsInventory[n].amount - 1;

                        for (int m = 0; m < MyStaticGameObjects.instance.potionItemUI.Count; m++)
                        {
                            if (MyStaticGameObjects.instance.potionItemUI[m].itemId == myItemsInventory[n].itemId)
                            {
                                Timing.RunCoroutine(MyStaticGameObjects.instance.potionItemUI[m].UseItem());
                                break;

                            }

                        }

                     
                    }
                    else
                    {
                        return useItem;
                    }
      
                }

                n++;
            }
            if (useItem)
            {
                SaveInventory();
            }
            return useItem;
        }

        /// <summary>
        /// Add item to the inVeontory
        /// </summary>
        /// <param name="theItems"></param>
        public void AddItems(List<Item> theItems)
        {
            int n = 0;
            int j = 0;
            bool found = false;
            List<Item> myItems = new List<Item>();

            while (j < theItems.Count)
            {
                found = false;
                while (n < myItemsInventory.Count)
                {
                    if (myItemsInventory[n].itemId == theItems[j].itemId)
                    {

                        found = true;
                        Item item = myItemsInventory[n];
                        item.amount = item.amount + theItems[j].amount;
                        myItemsInventory[n] = item;
                        for (int m = 0; m < MyStaticGameObjects.instance.potionItemUI.Count; m++)
                        {
                            if (MyStaticGameObjects.instance.potionItemUI[m].itemId == myItemsInventory[n].itemId)
                            {
                                MyStaticGameObjects.instance.potionItemUI[m].RefreshUI();
                                break;

                            }

                        }

                    }
                    n++;
                }
                if (!found)
                {
                    myItems.Add(theItems[j]);
                }
                j++;

            }
            myItemsInventory.AddRange(myItems);
            SaveInventory();
        }

        /// <summary>
        /// Save the inventory to the cloud
        /// </summary>
        public void SaveInventory()
        {
            if (itemContainer == null)
            {
                itemContainer = new ItemContainer(myItemsInventory);
            }
            else
            {
                itemContainer.items = myItemsInventory;
            }
            
            string json = JsonUtility.ToJson(itemContainer);
            // Debug.Log(json);

            if (MyGameService.instance != null)
            {
                MyGameService.instance.playerInventory = json;
                MyGameService.instance.SaveData();
            }

        }

        /// <summary>
        /// Stop the attack and Defense boost after 30 seconds
        /// </summary>
        private void ResetBoostStack()
        {

            if (MyGameService.instance != null)
            {
                if (MyGameService.instance.GetLastAttackBoostTimeSpan().TotalMinutes > 30)
                {
                    MyGameService.instance.attackBoostStack = 1;
                    MyGameService.instance.SaveData();
                }
                if (MyGameService.instance.GetLastDefenseBoostTimeSpan().TotalMinutes > 30)
                {
                    MyGameService.instance.defenseBoostStack = 1;
                    MyGameService.instance.SaveData();
                }
            }
        }

        /// <summary>
        /// Load player inventory from cloud
        /// </summary>
        public void LoadInventory()
        {
            if (itemContainer == null)
            {
                itemContainer = new ItemContainer(myItemsInventory);
            }
            else
            {
                itemContainer.items = myItemsInventory;
            }


            if (MyGameService.instance != null)
            {
                if (MyGameService.instance.playerInventory.Length > 0 && MyGameService.instance.playerInventory != "empty")
                {
                    itemContainer = JsonUtility.FromJson<ItemContainer>(MyGameService.instance.playerInventory);
                    myItemsInventory = itemContainer.items;
                    //  Debug.Log("------ MyCharacterController Inventory Loaded: " + MyCloudSaveServices.instance.playerInventory);
                    //Debug.Log("------ MyCharacterController Inverntory Count: " + itemContainer.items.Count);        

                    for (int m = 0; m < MyStaticGameObjects.instance.potionItemUI.Count; m++)
                    {
                        MyStaticGameObjects.instance.potionItemUI[m].FindItemInPlayerInventory(true);
                        MyStaticGameObjects.instance.potionItemUI[m].RefreshUI();
                    }
                }
                else
                {
                    //  Debug.Log("------ MyCharacterController Inventory Using Default Inventory because data is : " + MyCloudSaveServices.instance.playerInventory);
                    if (MyGameService.instance.isFirstTimeStart)
                    {
                        Debug.Log("SaveData for initStorage");
                        //Set gem to default amount since it is the first time user starts the game 
                        if (MyGameService.instance.gemAmount < 100)
                        {
                            MyGameService.instance.gemAmount = 100;
                        }
                        SaveInventory();
                    }
                }
            }

        }
 
       
        /// <summary>
        /// Takes damge base on defense and damage point
        /// </summary>
        /// <param name="damagePoint"></param>
        /// <returns></returns>
        protected internal override bool TakesDamage(int damagePoint)
        {
            if (!revevived)
            {
                if(hp>0)
                {
                    if (!shownDamageOverlay)
                    {
                        shownDamageOverlay = true;
                        damgageOverlayStartTime = Time.time;
                        MyStaticGameObjects.instance.damageOverlay.enabled = true;
                        //Timing.RunCoroutine(ShowDamageOverlay());
                    }
                }
            }
            return base.TakesDamage(damagePoint);
        }

        /// <summary>
        /// Start Weapon trail 
        /// </summary>
        public void StartWeaponTrail()
        {
            if (myWeapons != null)
            {
                myWeapons.ActivateWeaponTrail();
            }

            // xWeaponTrail.Activate();
        }

        /// <summary>
        /// Stop weapon trail 
        /// </summary>
        public void StopWeaponTrail()
        {
            if (myWeapons != null)
            {
                myWeapons.DeactiveWeaponTrail();
                //xWeaponTrail.Deactivate();
            }
        }

        /// <summary>
        /// Start Combo attack
        /// </summary>
        public void TurnOnWeaponAttackCombo()
        {
            animator.SetBool("InCombo", true);
            TurnOnWeaponAttack();
        }

        /// <summary>
        /// Start Attack 
        /// </summary>
        public override void TurnOnWeaponAttack()
        {
            StartWeaponTrail();
            isWeaponAttacking = true;

            if (myWeaponCollider != null)
            {
                myWeaponCollider.enabled = true;
            }
        }

        /// <summary>
        /// Stop Attack Combo
        /// </summary>
        public void TurnOffWeaponAttackCombo()
        {
            TurnOffWeaponAttack(1);
            animator.SetBool("InCombo", false);
            //collider.enabled = true;
        }

        /// <summary>
        /// Stop Attack Base on the AnimationLayer 
        /// comboState or regular attack
        /// </summary>
        /// <param name="animationLayerCheck"></param>
        public void TurnOffWeaponAttack(int animationLayerCheck = 0)
        {
            if (!IsInAnimation(comboState) && animationLayerCheck == 0)
            {
                TurnOffWeapon();
            }
            else if (!IsInAnimation(attackState) && !IsInAnimation(attack2State) && !IsInAnimation(attack3State) && !IsInAnimation(attack4State) && animationLayerCheck == 1)
            {
                TurnOffWeapon();
            }
        }

        /// <summary>
        /// Stop Attack
        /// </summary>
        private void TurnOffWeapon()
        {

            isWeaponAttacking = false;
            StopWeaponTrail();

            if (currentWeapon != null && myWeaponCollider != null)
            {
                myWeaponCollider.enabled = false;
            }
        }
      
        /// <summary>
        /// Play Atack Voice
        /// </summary>
        public override void AttackVoice()
        {
            if (attackVoiceSource != null)
            {
                if (UnityEngine.Random.Range(0, 10) % 3 == 0 && Time.time - lastAttackVoice > 2)
                {
                    attackVoiceSource.Play();
                    lastAttackVoice = Time.time;
                }
            }
        }

        /// <summary>
        /// Stop Wind Sound Effect
        /// </summary>
        public override void SpinningFXOff()
        {
            if (spinningAudioSource != null)
            {
                spinningAudioSource.Stop();
            }
        }
        /// <summary>
        /// Create WIND SFX  
        /// </summary>
        /// <param name="loop"></param>
        public override void SpinningFXOn(int loop = 0)
        {
            if (spinningAudioSource != null)
            {
                if (loop == 1)
                {
                    spinningAudioSource.Play(true);
                }
                else
                {
                    spinningAudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Play sound for wepaon impact
        /// </summary>
        public override void Hit()
        {
            if (weaponAudioSource != null)
            {
                weaponAudioSource.Play();
            }
      
        }
      
       /// <summary>
       ///  Handles healing, Regen and attack, defense buff and sound effects 
       /// </summary>
        public override void HealPlayers()
        {
            if (healerTarget != null)
            {
                if (healerSpell == HealerSpell.Heal)
                {
                    myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();
                    if (myCharacterStatsCached.GetHp() < myCharacterStatsCached.GetMaxHP())
                    {
                        if (myCharacterStatsCached.GetHp() <= 0)
                        {
                            MyStaticGameObjects.numberOfTimesReviveTarget++;
                            MyKillCountDisplay.instance.UpdateKillCountText();
                        }
                        else
                        {
                            MyStaticGameObjects.numberOfTimesHealedTarget++;
                        }
                        myCharacterStatsCached.GiveHealth(GetAttackPoint());
                        CreateHealParticle();

                        if (healFX != null && !isHealing)
                        {
                            isHealing = true;

                            Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, healFX));
                        }
                        if (weaponAudioSource != null && Time.time - lastHealTime > 2)
                        {
                            lastHealTime = Time.time;
                            weaponAudioSource.Play();
                        }
                        myCharacterStatsCached.UpdateHealthBar();
                    }
                }
                else if (healerSpell == HealerSpell.Regen)
                {
                    MyStaticGameObjects.numberOfTimeRegenCasted++;
                    myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();

                    if (!myCharacterStatsCached.hasRegen && myCharacterStatsCached.GetHp()>0)
                    {
                      
                        myCharacterStatsCached.StartRegen(GetAttackPoint());
                        if (regenFX != null)
                        {
                            Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, regenFX));
                        }

                    }
                }
                else if (healerSpell == HealerSpell.AttackBuff)
                {
                    MyStaticGameObjects.numberOfTimeAttackBuffCasted++;
                    myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();
                    if (!myCharacterStatsCached.hasAttackBuff && myCharacterStatsCached.GetHp() > 0)
                    {
                        myCharacterStatsCached.StartAttackBuff(GetAttackPoint());
                        //myCharacterStatsCached.hasAttackBuff = true;
                        //Timing.RunCoroutine(myCharacterStatsCached.StartAttackBuff(GetAttackPoint()));
                        if (powerUpFX != null)
                        {
                            Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, powerUpFX));
                        }

                    }
                }
                else if (healerSpell == HealerSpell.DefenseBuff)
                {
                    MyStaticGameObjects.numberOfTimeDefenseBuffCasted++;
                   
                    myCharacterStatsCached = healerTarget.GetComponent<MyCharacterStats>();
                    if (!myCharacterStatsCached.hasDefenseBuff && myCharacterStatsCached.GetHp() > 0)
                    {
                        myCharacterStatsCached.StartDefenseBuff(GetAttackPoint());
                        //myCharacterStatsCached.hasDefenseBuff = true;
                        //Timing.RunCoroutine(myCharacterStatsCached.StartDefenseBuff(GetAttackPoint()));

                        if (shieldFX != null)
                        {

                            Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, shieldFX));

                        }

                    }
                }
            }
        }

        /// <summary>
        /// Create VFX for healing at the healing target position 
        /// </summary>
        private void CreateHealParticle()
        {
            particle = MyObjectPools.healParticlePoolTransform.GetChild(MyObjectPools.instance.healParticlePool.GetMyObjectIndex()).gameObject;
            particle.transform.position = healerTarget.transform.position + particleOffset;
            particle.GetComponent<ParticleSystem>().Play(true);
        }
     
        /// <summary>
        /// Create VFX for Sword Impact with monster
        /// </summary>
        public void SwordImpact()
        {
            particle = MyObjectPools.weaponImpactPool.transform.GetChild(MyObjectPools.weaponImpactPool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;
        }

        /// <summary>
        /// Create VFX for Skill animation
        /// </summary>
        public void SkillFX()
        {
            Vector3 offset= new Vector3(0,.2f,0);
            if(skillParticlePool!=null){
                GameObject particle = skillParticlePool.GetMyObject();
                particle.GetComponent<ParticleSystem>().Play(true);
                particle.transform.position = transform.position + offset;
            }
        }
    }
}