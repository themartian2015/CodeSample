using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using MEC;
using XftWeapon;


namespace MiniLegend.CharacterStats
{
    /// <summary>
    /// This Class controls the monster gameobject's movement, animations and behavior  
    /// </summary>
    public class MyMonsterNavMeshAgent : MyCharacterStats
    {
        protected internal bool isBoss = false;
        public bool pendingSpawn = false;
        public static Vector3 storagePoolLocation = new Vector3(0, -985.3231f, 0);
        public static float reCalculateDistance = 1.0f;
        public bool isActive = false;
        public enum MonsterType { Bandit, Assassin, Succubus, DarkKnight, Werewolf,Demon };
        public enum MonsterMode { SEARCHANDDESTROY = 0, DEFEND = 1 };

        public MonsterType monsterType;
        public MonsterMode monsterMode = MonsterMode.DEFEND;
        public MeshRenderer[] meshRenderers;
        public SkinnedMeshRenderer[] skinnedMeshRenderers;
        public bool isTargetInMeleeRange = false;
        public bool isTargetInRangeAttackRange = false;

        public MyXWeaponTrail xWeaponTrailR;
        public MyXWeaponTrail xWeaponTrailL;
        public GameObject currentClosestCompanionOrHouse;
    
        private Vector2 previousTargetVector = Vector3.zero;
        private Vector2 tempVector2 = Vector2.zero;
        private List<string> collisionGameObjects = new List<string>();
        public int baseKillExp = 25;
        public int killEXP = 25;
        public int divideExp = 0;
        public float playerStopDistance = 3f;
        public GameObject spark;
        public GameObject particlePoint;
        public bool hasRangeAttack = false;
        public bool findingTarget = false;
        public MyMonsterObjectPool objectPool;
        public AudioSource ufootStepAudioSource;
        public AudioSource uonHitAudioSource;
        public AudioSource uweaponAudioSource;
        public static bool hasFinalSceneStarted = false;
        public float power = 5f;
        public float radius = 10f;
        public string spawnPoint = "";
        public Item[] monsterItems;
        public bool isPoison = false;
        public bool isSlowDown = false;
        public GameObject headObject;
        public Collider weaponCollider;
        public Collider weaponCollider2;
        public Collider targetCollider;
        public Collider meleeCollider;
        public bool isLeader = false;
        private AnimatorStateInfo currentBaseLayerStateInfo;
        private int randomNumber = 0;
        private bool isDead = false;
        private bool toggleCollider = false;
        private int index = 0;
        private int lastColliderID = 0;
        private int weaponAudioIndex = 0;
        private int footstepsAudioIndex = 0;
        private int onHitAudioIndex = 0;
        private int meleeAttackForce = 0;

        private Vector3 bagPosition = Vector3.zero;
        private float defaultSpeed = 0f;
        private float defaultNavMeshSpeed = 0f;
        private int countDown = 0;
        private float difficultyAdjustMultipler = 1f;
        private CoroutineHandle coroutineFindTarget;
        private CoroutineHandle coroutineMovement;
        private bool startedMovementCoroutine = false;
        private GameObject posionParticle;
        private GameObject slowParticle;
        private GameObject magicBallParticle;
        private int indexHit = 0;
        private float alpha = 0f;
        private Color defaultFadeColor = new Color(1f, 1f, 1f, 0f);
        private Color defaultFadeOutColor = new Color(1f, 1f, 1f, 1f);

        private MyCharacterStats myCharacterStatsCache;
        private MyCompanionRangeWeapon myCompanionRangeWeaponCache;
        private static string knockDownForceStr = "KnockDownForce";
        private static string monsterString = "";
        private static Item[] gemItems = new Item[1];
      
        private Vector3 heading;
        private float distance;
        private float distancePreviousTarget;
        private Vector3 direction;

        private int attackIncreaseAmount = 0;
        private int defenseIncreaseAmount = 0;
        private float lastCollisionTime = 0;
        private Vector3 tempVector;
        private float movementControlInterval = .25f;
        private Vector3 rotationDirection; 
        public static int targetIndex = 0;
        private int monsterIndex = 0;
        private int monsterTypes = 6;
        private GameObject skinMeshGameObject;
        private static string wolfQuestId = "WEREWOLFCLAWQUEST";
        private Vector3 vectorZero = Vector3.zero;
        private Vector3 rayCastOffSet = new Vector3(0, 4, 0);
        protected static MonsterStats[][] monsterStats = new MonsterStats[11][];
        public static bool resetMonsterStats = true;
        private MyMonsterRangeWeapon myMonsterRangeWeapon;
        private NavMeshHit navMeshHit;
       
        private int startHealth = 0;
        private int randomNum = 0;

        private string terrainStr = "Terrain";
        private string caveStr = "Cave";
        private Vector3 currentRot = Vector3.zero;
        private float posionStartTime = 0;
        private float slowStartTime = 0;
        private float toggleStartTime = 0;
        private int posionDamageAmount = 0;
        private float arrowDistance = 0;

        private Transform targetTransform;
        private Transform headTransform;
        private MyCharacterStats targetCharacterStats;
        private MeshRenderer blobShadowMeshRenderer;
        private bool playFootStepSound = true;
        private GameObject collisonObject = null;
        private int walkableAreaMask = 0;
        private float findTargetInterval = 1f;
        private float defaultMovementControlInterval = .1f;
        private MyMonsterFixedUpdate myMonsterFixedUpdate;
        
        public struct MonsterStats
        {
            public MonsterStats(int theHp,int theAttackPoint, int theDefensePoint, int theKillExp,float theNavMeshSpeed,int theDivideExp)
            {
                hp = theHp;
                attackPoint = theAttackPoint;
                defensePoint = theDefensePoint;
                killExp = theKillExp;
                navMeshSpeed = theNavMeshSpeed;
                divideExp = theDivideExp;
            }
            public int hp;
            public int attackPoint;
            public int defensePoint;
            public int killExp;
            public int divideExp;
            public float navMeshSpeed;
        }
       
        ///<summary>
        /// Return the monster name from the monster type
        /// </summary>
        /// <returns>name of the monster</returns>
        /// <param name="monstertype"> MonsterType</param>
        public static string MonsterTypeToString(MonsterType monsterType)
        {
            monsterString = "";
            switch (monsterType)
            {
                case MonsterType.Bandit:
                    monsterString = "Bandit";
                    break;
                case MonsterType.Assassin:
                    monsterString = "Assassin";
                    break;
                case MonsterType.Succubus:
                    monsterString = "Succubus";
                    break;
                case MonsterType.DarkKnight:
                    monsterString = "Dark Knight";
                    break;
                case MonsterType.Werewolf:
                    monsterString = "Werewolf";
                    break;
                case MonsterType.Demon:
                    monsterString = "King Snow";
                    break;
            }
            return monsterString;
        }

        /// <summary>
        /// Initalize the monster and check to see if this is the boss
        /// </summary>
        private void Start()
        {
            Init();
            if (CompareTag(MyStaticGameObjects.bossString))
            {
                isBoss = true;
            }
            else
            {
                isBoss = false;
            }
        }
      
        /// <summary>
        /// Level Up the monster increasing its attack, defense and speed each level
        /// </summary>
        protected override void LevelUp()
        {
            level = level + 1;
            hp = monsterStats[monsterIndex][level].hp;
            attackPoint = monsterStats[monsterIndex][level].attackPoint;
            defensePoint = monsterStats[monsterIndex][level].defensePoint;
            killEXP = monsterStats[monsterIndex][level].killExp;
            divideExp = monsterStats[monsterIndex][level].divideExp;
            defaultNavMeshSpeed = navMeshAgent.speed = monsterStats[monsterIndex][level].navMeshSpeed;
          
        }
       
        /// <summary>
        /// Generate the monster stats for 25 level and cached the result. stats such as HP, Attack Point, Defense Point, Exp
        /// </summary>
        protected internal void CalculateLevelUp()
        {
            float hpDifficultyAdjustMultipler = 1f;

            if (monsterStats[monsterIndex] == null)
            {
               // Debug.Log("create Stats Array:" + gameObject.name) ;
                monsterStats[monsterIndex] = new MonsterStats[26];

                for(int x= 0; x< 26; x++)
                {
                  
                    int theHp = baseHp + MyStaticGameObjects.instance.GetMyLevelUpEquation(x);
                    int theAttackPoint = 0;
                    int theDefensePoint = 0;
                    int theKillEXP = 0;
                    int theDivideExp = 0;
                    float theNavMeshSpeed = navMeshAgent.speed;

                    if (x > 2 && x < 7)
                    {
                        attackIncreaseAmount = 5;
                        defenseIncreaseAmount = 1;
                    }
                    if (x >= 7 && x < 10)
                    {
                        attackIncreaseAmount = 6;
                        defenseIncreaseAmount = 1;
                    }
                    else if (x >= 10 && x < 15)
                    {
                        attackIncreaseAmount = 11;
                        defenseIncreaseAmount = 4;
                    }
                    else if (x >= 15 && x < 20)
                    {
                        attackIncreaseAmount = 12;
                        defenseIncreaseAmount = 4;
                    }
                    else if (x >= 20 && x < 30)
                    {
                        attackIncreaseAmount = 13;
                        defenseIncreaseAmount = 4;
                    }
                    else if (x >= 30 && x < 40)
                    {
                        attackIncreaseAmount = 17;
                        defenseIncreaseAmount = 5;
                    }
                    else if (x >= 40)
                    {
                        attackIncreaseAmount = 20;
                        defenseIncreaseAmount = 5;
                    }
                    if (x > 5)
                    {
                        if (navMeshAgent.speed < 8)
                        {
                            theNavMeshSpeed = navMeshAgent.speed = navMeshAgent.speed + .13f;
                        }else if (navMeshAgent.speed >= 8 && navMeshAgent.speed<9)
                        {
                            theNavMeshSpeed = navMeshAgent.speed = navMeshAgent.speed + .08f;
                        }
                    }

                    theAttackPoint = baseAttackPoint + MyStaticGameObjects.instance.GetMyLevelUpEquation(x) + attackIncreaseAmount;
                    theDefensePoint = baseDefensePoint + MyStaticGameObjects.instance.GetMyLevelUpEquation(x) + defenseIncreaseAmount;
                    theKillEXP = baseKillExp + MyStaticGameObjects.instance.GetMyLevelUpEquation(x);

                    int difficultyIndex = 1;
                    if (MyCharacterSelector.instance == null)
                    {
                        //Debug.Log("MyCharacterSelector.instance is null");
                    }
                    else if (MyCharacterSelector.instance != null)
                    {
                        difficultyIndex = MyCharacterSelector.instance.difficultyIndex;
                    }
                    switch (difficultyIndex)
                    {
                        case 0:
                            //easy
                            difficultyAdjustMultipler = 1.3f;
                            hpDifficultyAdjustMultipler = 1.5f;
                            theHp = Mathf.RoundToInt((float)theHp * hpDifficultyAdjustMultipler);
                            theAttackPoint = Mathf.RoundToInt((float)theAttackPoint * difficultyAdjustMultipler);
                            theDefensePoint = Mathf.RoundToInt((float)theDefensePoint * difficultyAdjustMultipler);
                            break;
                        case 1:
                            //Normal
                            difficultyAdjustMultipler = 1.6f;
                            hpDifficultyAdjustMultipler = 2.0f;
                            theHp = Mathf.RoundToInt((float)theHp * hpDifficultyAdjustMultipler);
                            theAttackPoint = Mathf.RoundToInt((float)theAttackPoint * difficultyAdjustMultipler);
                            theDefensePoint = Mathf.RoundToInt((float)theDefensePoint * difficultyAdjustMultipler);
                            break;
                        case 2:
                            //Hard
                            difficultyAdjustMultipler = 2.0f;
                            hpDifficultyAdjustMultipler = 2.7f;
                            theHp = Mathf.RoundToInt((float)theHp * hpDifficultyAdjustMultipler);
                            theAttackPoint = Mathf.RoundToInt((float)theAttackPoint * difficultyAdjustMultipler);
                            theDefensePoint = Mathf.RoundToInt((float)theDefensePoint * difficultyAdjustMultipler);
                            break;
                        case 3:
                            //Impossible
                            difficultyAdjustMultipler = 2.5f;
                            hpDifficultyAdjustMultipler = 3.2f;
                            theHp = Mathf.RoundToInt((float)theHp * hpDifficultyAdjustMultipler);
                            theAttackPoint = Mathf.RoundToInt((float)theAttackPoint * difficultyAdjustMultipler);
                            theDefensePoint = Mathf.RoundToInt((float)theDefensePoint * difficultyAdjustMultipler);
                            break;
                    }
                    theDivideExp = (int)((float)theKillEXP / (float)4);
                    //Debug.Log("difficultyIndex:" + difficultyIndex + " " +  monsterType.ToString() + " Level:"  + x + " HP: " + theHp + " Atk:" + theAttackPoint  + " Def:" + theDefensePoint + " KillExp:" + theKillEXP + " TheNavMeshSpeed:" + theNavMeshSpeed);
                    MonsterStats monsterStatsStruct = new MonsterStats(theHp, theAttackPoint, theDefensePoint, theKillEXP, theNavMeshSpeed, theDivideExp);
                    monsterStats[monsterIndex][x] = monsterStatsStruct;

                   
                }
           
            }
        }
       
        /// <summary>
        /// Trigger Every second by the Update method 
        /// Take damage caused by Posion and Slow or reset it
        /// </summary>
        protected override internal void OneSecondInterval()
        {
            if (Time.time - oneSecondInterval > 1)
            {
                oneSecondInterval = Time.time;
                if (isPoison)
                {
                    if (Time.time - posionStartTime < 10 && isActive && hp > 0)
                    {
                        TakesDamage(posionDamageAmount);
                
                    }
                    else
                    {
                        StopPoisonDamage();
                        startHealth = hp;
                    }
                    if (hp <= 0 && isActive && startHealth > 0)
                    {
                        StopPoisonDamage();
                        DeadMonster();
                        if (MyStaticGameObjects.instance.player.GetComponent<MyBowController>() != null)
                        {
                            MyStaticGameObjects.playerKillCount++;
                            MyKillCountDisplay.instance.UpdateKillCountText();
                        }
                    }
                }

                if (isSlowDown)
                {
                    if (Time.time - slowStartTime > 10)
                    {
                        animator.speed = defaultSpeed;
                        navMeshAgent.speed = defaultNavMeshSpeed;
                        isSlowDown = false;
                        if (slowParticle != null)
                        {
                            slowParticle.GetComponent<MyAttachableParticles>().StopFollow();
                        }
                    }
                }
                if (toggleCollider && Time.time - toggleStartTime > 1)
                {
                    meleeCollider.enabled = true;
                    toggleCollider = false;
                }

            }
        }
      
        /// <summary>
        /// Reset the poison status
        /// </summary>
        private void StopPoisonDamage()
        {
            posionDamageAmount = 0;
            isPoison = false;
     
            if (posionParticle != null)
            {
                posionParticle.GetComponent<MyAttachableParticles>().StopFollow();
            }
        }
       
        /// <summary>
        /// Reset both poison and slow Status
        /// </summary>
        private void ResetPosionAndSlowStatus()
        {
            animator.speed = defaultSpeed;
            navMeshAgent.speed = defaultNavMeshSpeed;
            isSlowDown = false;
            if (slowParticle != null)
            {
                slowParticle.GetComponent<MyAttachableParticles>().StopFollow();
            }

            StopPoisonDamage();
        }
       
        /// <summary>
        /// Match the monster's level with the main character's level - 1 
        /// </summary>
        protected internal void MatchPlayerLevel()
        {
            if (myCharacterStatsCache != null)
            {
                level = myCharacterStatsCache.GetLevel() -1;
            }
            else
            {
                level = MyStaticGameObjects.instance.player.GetComponent<MyCharacterController>().GetLevel()-1;
            }
            LevelUp();
        }
        
        /// <summary>
        /// Returns the Attack Point 
        /// </summary>
        /// <returns></returns>
        public int GetAttackPoint()
        {
            return attackPoint;
        }
        
        /// <summary>
        /// Returns the Defense Point
        /// </summary>
        /// <returns></returns>
        public int GetDefensePoint()
        {
            return defensePoint;
        }
       
        /// <summary>
        /// Get the Max HP for the current Level 
        /// </summary>
        /// <returns></returns>
        public override int GetMaxHP()
        {
            return monsterStats[monsterIndex][level].hp;
        }
        
        /// <summary>
        /// initialize the monster gameobject speed, calculate level stats, change shader based on quality level in Unity settings 
        /// </summary>
        protected override void Init()
        {
            if (!isInitialized)
            {
                base.Init();
               
                target = null;
                targetTransform = null;
                targetCharacterStats = null;
                defaultSpeed = animator.speed;
                defaultNavMeshSpeed = navMeshAgent.speed;
                monsterIndex = (int)monsterType;
               
                if (isLeader)
                {
                    monsterIndex = monsterIndex + monsterTypes;
                }
                if (resetMonsterStats)
                {
                    resetMonsterStats = false;
                    monsterStats = new MonsterStats[11][];
                }
                CalculateLevelUp();
                if (isBoss)
                {

                    defaultMovementControlInterval =  movementControlInterval = .10f;
                    if (MyStaticGameObjects.instance.debugOn)
                    {
                        level = MyStaticGameObjects.debugLevel;
                        LevelUp();
                    }
                }
                else
                {
                    defaultMovementControlInterval =  movementControlInterval = Random.Range(.25f, .5f);
                }

                if(QualitySettings.GetQualityLevel() < 2)
                {
                    playFootStepSound = false;
                }

                level = 0;
                navMeshAgent.speed = defaultNavMeshSpeed;
                if (headObject != null)
                {
                    headTransform = headObject.transform;
                }
                else
                {
                    headTransform = theTransform;
                }
                if (MyCrossFadeAudioMixer.instance != null)
                {
                    onHitAudioIndex = Random.Range(0, MyCrossFadeAudioMixer.instance.onHit.Length);
                    footstepsAudioIndex = Random.Range(0, MyCrossFadeAudioMixer.instance.footSteps.Length);
                }
                randomNum = Random.Range(1, 10);
                walkableAreaMask = 1 << NavMesh.GetAreaFromName(MyStaticGameObjects.walkableAreaStr);

                if (myBlobShadow != null)
                {
                    blobShadowMeshRenderer = myBlobShadow.GetComponent<MeshRenderer>();
                    if (!isBoss && blobShadowMeshRenderer != null)
                    {
                        blobShadowMeshRenderer.enabled = false;
                    }
                }
                myMonsterFixedUpdate = GetComponent<MyMonsterFixedUpdate>();
                GetSkinMeshRenderer();
                InitWeaponTrail();
                TurnOffWeaponAttack();

                if(gemItems[0] == null)
                {
                    Item gemItem = new Item();
                    gemItem.itemType = MyStorageItems.ItemType.QUESTITEM;
                    gemItem.displayName = "Gem";
                    gemItem.amount = 1;
                    gemItem.cost = 0;
                    gemItem.description = "Gem";
                    gemItem.itemId = MyStorageItems.ItemsId.GEM;
                    gemItems[0] = gemItem;
                }
            }
        }
       
        /// <summary>
        /// return the skinMeshRenderer and change the shader based on quality
        /// </summary>
        protected internal void GetSkinMeshRenderer()
        {
            skinMeshGameObject = MyStaticGameObjects.instance.ChangeShaderQuality(GetComponentsInChildren<SkinnedMeshRenderer>());
            MyStaticGameObjects.instance.ChangeShaderQuality(GetComponentsInChildren<MeshRenderer>(true));
        }
      
        /// <summary>
        /// Change the monster mode.
        /// Search and Destory mode, monster will actively search for the players and attack 
        /// Defend mode,  monster will only attack if the player come within range 
        /// </summary>
        /// <param name="theMonsterMode"></param>
        protected internal void SwitchMonsterMode(MonsterMode theMonsterMode)
        {
            monsterMode = theMonsterMode;
            if (monsterMode == MonsterMode.SEARCHANDDESTROY)
            {
                if (monsterType == MonsterType.Succubus)
                {
                    targetCollider.enabled = true;
                }
                else
                {
                    targetCollider.enabled = false;
                }
                if (coroutineFindTarget.IsValid)
                {
                    Timing.ResumeCoroutines(coroutineFindTarget);
                }
                else
                {
                    coroutineFindTarget = Timing.RunCoroutine(NavMeshControl());
                }
            }
            else
            {
                targetCollider.enabled = true;
            }
            meleeCollider.enabled = true;
        }
        
        /// <summary>
        /// Triggers at an interval based on movementControlInterval
        /// calls the MovementControlAndAnimationCheck method
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> MovementCoroutine()
        {
            startedMovementCoroutine = true;
            while (gameObject.activeSelf)
            {
                MovementControlAndAnimationCheck();
                yield return Timing.WaitForSeconds(movementControlInterval);
            }
            startedMovementCoroutine = false;
        }

        /// <summary>
        /// Calls the FindTarget based on the findTargetInterval
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> NavMeshControl()
        {
            while (isActive)
            {
                if (isActive && hp>0 && !hasFinalSceneStarted && target == null && !findingTarget)
                {
                    FindTarget();
                }
                yield return Timing.WaitForSeconds(findTargetInterval);
            }
        }
       
        /// <summary>
        /// Set the attack target GameObject
        /// </summary>
        /// <param name="theTarget"></param>
        protected internal override void SetTarget(GameObject theTarget)
        {
            if (hp < 1)
                return;

            target = theTarget;
            if (null != target)
            {
                targetTransform = target.transform;
                targetCharacterStats = target.GetComponent<MyCharacterStats>();
            }
            else
            {
                targetTransform = null;
                targetCharacterStats = null;
                if (monsterMode == MonsterMode.DEFEND)
                {
                    targetCollider.enabled = true;
                    if (!toggleCollider)
                    {
                        toggleCollider = true;
                        if (meleeCollider != null)
                        {
                            meleeCollider.enabled = false;
                        }
                        toggleStartTime = Time.time;
                    }
                }
                else
                {
                    if (coroutineFindTarget.IsValid)
                    {
                        Timing.ResumeCoroutines(coroutineFindTarget);
                    }
                }
            }
            if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;

                if (null != target && isActive)
                {
                    navMeshAgent.nextPosition = theTransform.position;
                    navMeshAgent.isStopped = false;
                    navMeshAgent.SetDestination(targetTransform.position);
                    navMeshAgent.updateRotation = true;
                    if (coroutineMovement.IsValid)
                    {
                        Timing.ResumeCoroutines(coroutineMovement);
                    }
                    else
                    {
                        coroutineMovement = Timing.RunCoroutine(MovementCoroutine());
                    }
                  

                }
            }

        }
        
        /// <summary>
        /// Find the attack target 
        /// </summary>
        void FindTarget()
        {
            if (monsterMode == MonsterMode.SEARCHANDDESTROY)
            {
                if (target == null && !findingTarget)
                {
                    findingTarget = true;
                    randomNumber = targetIndex;
                    
                    if (MyStaticGameObjects.instance.companionAndPlayerStats[randomNumber].GetHp() > 0)
                    {
                        SetTarget(MyStaticGameObjects.instance.companionsAndPlayer[randomNumber]);
                        Timing.PauseCoroutines(coroutineFindTarget);
                    }
                    findingTarget = false;
                    targetIndex++;
                    if (targetIndex > 3)
                    {
                        targetIndex = 0;
                    }
                }
            }
        }
       
        /// <summary>
        /// Controls the monster's movement and Animations
        /// </summary>
        void MovementControl()
        {
            if (isActive)
            {
                if (isBoss)
                {
                    if (!hasFinalSceneStarted && hp < 30)
                    {
                        TurnOffWeaponAttack();
                        if (MyBossHPUI.myBossHPUIInstance != null)
                        {
                            MyBossHPUI.myBossHPUIInstance.gameObject.SetActive(false);
                        }

                        MyStaticGameObjects.instance.hpPanel.SetActive(false);
                        MyStaticGameObjects.instance.itemPanel.SetActive(false);
                       
                        animator.SetBool("Attack1", false);
                        animator.SetBool("Attack2", false);
                        animator.SetBool("Attack3", false);
                        animator.SetBool("Run", false);
                        animator.SetBool("Injured", true);
                        hasFinalSceneStarted = true;
                        MyStaticGameObjects.instance.finalCutSceneObject.SetActive(true);
                        MyStaticGameObjects.instance.finalCutSceneObject.GetComponent<MyFinalCutScene>().StartFinalScene();
                        target = null;
                        this.enabled = false;
                        return;
                    }
                }
                else
                {

                    if (hp <= 0 && !isDead)
                    {
                        DeathAnimation();
                        ResetPosionAndSlowStatus();
                        Timing.RunCoroutine(FadeObject());
                        SetTarget(null);
                        isDead = true;
                        return;
                    }
                }

                OneSecondInterval();
                CalculateBlogShadowHeight();

                if (target != null)
                {
                    tempVector2.x = targetTransform.position.x;
                    tempVector2.y = targetTransform.position.z;

                    distancePreviousTarget = (tempVector2 - previousTargetVector).sqrMagnitude;

                    if (targetCharacterStats == null || targetCharacterStats.GetHp() < 1)
                    {
                        ResetTarget();

                        return;
                    }
                    else if (distancePreviousTarget > reCalculateDistance)
                    {
                        if (navMeshAgent.enabled && !hasFinalSceneStarted)
                        {
                            navMeshAgent.SetDestination(targetTransform.position);                      
                            previousTargetVector.x = targetTransform.position.x;
                            previousTargetVector.y = targetTransform.position.z;
                        }
                    }

                    if (!hasFinalSceneStarted && hp > 0)
                    {
                        if (navMeshAgent.velocity.x != 0 || navMeshAgent.velocity.z != 0)
                        {
                            RunAnimation();
                            movementControlInterval = defaultMovementControlInterval;
                        }
                        else if (hasRangeAttack && isTargetInRangeAttackRange || !hasRangeAttack && isTargetInMeleeRange)
                        {
                            if (hasRangeAttack)
                            {
                                rotationDirection = (targetTransform.position - theTransform.position).normalized;
                                rotationDirection.y = 0;
                                theTransform.rotation = Quaternion.Slerp(theTransform.rotation, Quaternion.LookRotation(rotationDirection), 30);
                            }
                            AttackAnimation();
                            movementControlInterval = defaultMovementControlInterval + .25f;
                        }
                        else
                        {
                            IdleAnimation();
                            movementControlInterval = defaultMovementControlInterval;
                        }
                    }

                }
                else
                {
                    IdleAnimation();
                    movementControlInterval = defaultMovementControlInterval;
                }
            }
        }
       
        /// <summary>
        /// Reset the attack target to null
        /// </summary>
        protected internal void ResetTarget()
        {
            IdleAnimation();
            isTargetInMeleeRange = false;
            isTargetInRangeAttackRange = false;
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.updatePosition = true;
                navMeshAgent.isStopped = false;
                navMeshAgent.updateRotation = true;
            }
            rigidbody.isKinematic = true;
            SetTarget(null);
        }
        
        /// <summary>
        /// Call after the MovementControl to disable the navmesh control if the character is dead or attacking
        /// or enable the navmesh control if the character needs to move 
        /// </summary>
        private void MovementControlAndAnimationCheck()
        {
            if (isActive)
            {
                MovementControl();
                if (IsInAnimation(MyAnimationStates.knockDownState) || IsInAnimation(MyAnimationStates.getUpState) || IsInAnimation(MyAnimationStates.attackState) || IsInAnimation(MyAnimationStates.dieState) || IsInAnimation(MyAnimationStates.takeDamageState) || IsInAnimation(MyAnimationStates.attack2State))
                {
                    navMeshAgent.updateRotation = false;
                    navMeshAgent.updatePosition = false;
                }
                else if (Time.time - lastCollisionTime > .25f && (!navMeshAgent.updatePosition|| !navMeshAgent.updateRotation))
                {
                    navMeshAgent.nextPosition = theTransform.position;
                    rigidbody.isKinematic = true;
                    navMeshAgent.updatePosition = true;
                    navMeshAgent.updateRotation = true;
                }
            }

        }
       
        /// <summary>
        /// Reset all Animation and use default animation
        /// </summary>
        protected internal void IdleAnimation()
        {
            if (animator.GetBool("Run"))
            {
                animator.SetBool("Run", false);
            }
            if (animator.GetBool("Attack1"))
            {
                animator.SetBool("Attack1", false);
            }
            if (hasRangeAttack && animator.GetBool("Attack2"))
            {
                animator.SetBool("Attack2", false);
            }
            TurnOffWeaponAttack();
            if (isBoss)
            {
                if (animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }
                if (animator.GetBool("Attack3"))
                {
                    animator.SetBool("Attack3", false);
                }
            }
        }
       
        /// <summary>
        /// Reset all Animation and Set Melee Attack animation
        /// </summary>
        protected internal void AttackAnimation()
        {

            if (!IsInAnimation(MyAnimationStates.attackState) && !IsInAnimation(MyAnimationStates.attack2State) && !IsInAnimation(MyAnimationStates.attack3State))
            {
                //boss attacks (Combo1,2,5)
                if (isBoss)
                {

                    switch (Random.Range(0, 2))
                    {
                        case 0:
                            if (!animator.GetBool("Attack1"))
                            {
                                theTransform.LookAt(targetTransform.position);
                                animator.SetBool("Attack1", true);
                            }
                            break;
                        case 1:
                            if (!animator.GetBool("Attack2"))
                            {
                                theTransform.LookAt(targetTransform.position);
                                animator.SetBool("Attack2", true);
                            }
                            break;
                        case 2:
                            if (!animator.GetBool("Attack3"))
                            {
                                theTransform.LookAt(targetTransform.position);
                                animator.SetBool("Attack3", true);
                            }
                            break;
                        default:
                            if (!animator.GetBool("Attack1"))
                            {
                                theTransform.LookAt(targetTransform.position);
                                animator.SetBool("Attack1", true);
                            }
                            break;
                    }
                }
                else
                {
                    if (!animator.GetBool("Attack1"))
                    {
                        theTransform.LookAt(targetTransform.position);
                        animator.SetBool("Attack1", true);
                    }
                }
                if (animator.GetBool("Run"))
                {
                    animator.SetBool("Run", false);
                }
                if (hasRangeAttack && animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }

            }
        }
        
        /// <summary>
        /// Reset all Animation and set the Range Attack animation 
        /// </summary>
        protected internal void Attack2Animation()
        {

            if (!IsInAnimation(MyAnimationStates.attack2State))
            {
                if (animator.GetBool("Attack1"))
                {
                    animator.SetBool("Attack1", false);
                }
                if (hasRangeAttack && !animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", true);
                }
                if (animator.GetBool("Run"))
                {
                    animator.SetBool("Run", false);
                }
            }
        }
      
        /// <summary>
        /// Reset All animation and set the Run animation 
        /// </summary>
        private void RunAnimation()
        {
            if (!animator.GetBool("Run"))
            {
                animator.SetBool("Run", true);
            }
            if (animator.GetBool("Attack1"))
            {
                animator.SetBool("Attack1", false);
            }
            if (isBoss)
            {
                if (animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }
                if (animator.GetBool("Attack3"))
                {
                    animator.SetBool("Attack3", false);
                }
            }
            if (hasRangeAttack && animator.GetBool("Attack2"))
                animator.SetBool("Attack2", false);
        }
       
        /// <summary>
        /// Set Knockdown animation 
        /// </summary>
        private void KnockDownAnimation()
        {
            animator.SetTrigger("KnockDown");
        }
        
        /// <summary>
        /// Set take Damage Animation 
        /// </summary>
        private void TakeDamageAnimation()
        {
            animator.SetTrigger("TakeDamage");
        }
       
        /// <summary>
        /// Reset all Animations and Set the Death animation 
        /// </summary>
        private void DeathAnimation()
        {
            if (!animator.GetBool("Die"))
            {
                animator.SetBool("Die", true);
            }
            if (animator.GetBool("Attack1"))
            {
                animator.SetBool("Attack1", false);
            }
            if (isBoss)
            {
                if (animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }
                if (animator.GetBool("Attack3"))
                {
                    animator.SetBool("Attack3", false);
                }
            }
            if (animator.GetBool("Run"))
            {
                animator.SetBool("Run", false);
            }
            if (hasRangeAttack && animator.GetBool("Attack2"))
                animator.SetBool("Attack2", false);

        }

        /// <summary>
        /// Returns true if the damage point is greater than the defense point and subtract from HP
        /// Return false if the damage point is equal or less then the defense point 
        /// Updates the boss Health bar
        /// </summary>
        /// <param name="damagePoint">Damage</param>
        /// <returns>bool</returns>
        protected internal override bool TakesDamage(int damagePoint)
        {
            hit = false;
            if (isActive)
            {
                damage = defensePoint - damagePoint;
                if (damage < 0)
                {
                    hp = hp + damage;
                    if (!isBoss)
                    {
                        TakeDamageAnimation();
                    }
                    else
                    {
                        if (MyBossHPUI.myBossHPUIInstance != null)
                        {
                            MyBossHPUI.myBossHPUIInstance.UpdateHealthUI();
                        }
                    }
                    hit = true;
                }
                else
                {
                    hit = false;
                }
            }
            return hit;


        }
       
        /// <summary>
        /// When collision occurs and caused by players's weapons then call takes damage or dead animation 
        /// Create onHit animation or miss particles
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision)
        {
            startHealth = hp;
            
            indexHit = 0;
            while (indexHit < collision.contacts.Length)
            {
                collisonObject = collision.contacts[indexHit].otherCollider.gameObject;
                if (lastColliderID == collisonObject.GetInstanceID())
                {
                    break;
                }
                lastColliderID = collisonObject.GetInstanceID();
         
                ///<summary>player's melee weapons</summary>
                if (collisonObject.layer == MyStaticGameObjects.playerWeaponLayer || collisonObject.layer == MyStaticGameObjects.companionWeaponLayer)
                {
                    if (lastColliderID == MyCharacterController.knockDownWeaponId)
                    {
                        myCharacterStatsCache = MyStaticGameObjects.instance.companionAndPlayerStats[0];
                    }
                    else
                    {
                        myCharacterStatsCache = collisonObject.transform.root.GetComponent<MyCharacterStats>();
                    }
                    if (myCharacterStatsCache.IsWeaponAttacking())
                    {
                        if (!TakesDamage(myCharacterStatsCache.GetAttackPoint()))
                        {
                            //Miss particles
                            CreateMissParticle(collision.contacts[indexHit].point);
                        }
                        else
                        {
                            OnHitSound();
                            CreateOnHitParticle(collision.contacts[indexHit].point);
                            if (startHealth > 0) {
                                if (myCharacterStatsCache.gameObject.layer == MyStaticGameObjects.playerLayer)
                                {
                                   ((MyCharacterController)myCharacterStatsCache).proformAttack = false;
                                   MyStaticGameObjects.attackHitTarget++;
                                }
                             
                                if (lastColliderID == MyCharacterController.knockDownWeaponId)
                                {
                                    if (!isBoss)
                                    {
                                        ApplyForce(3500, collision.contacts[indexHit].otherCollider.transform.position, collision.contacts[indexHit].normal);
                                    }
                                    KnockDownAnimation();
                                }
                                else
                                {
                                    if (!isBoss)
                                    {
                                        ApplyForce(3000, collision.contacts[indexHit].otherCollider.transform.position, collision.contacts[indexHit].normal);
                                    }
                                    else
                                    {
                                        ApplyForce(1000, collision.contacts[indexHit].otherCollider.transform.position, collision.contacts[indexHit].normal);
                                    }
                                }
                            }
                        }

                        if (hp < 1 && startHealth>0 && !isBoss)
                        {
                            DeadMonster();
                          
                            collider.enabled = false;
                            if (myCharacterStatsCache.gameObject.layer == MyStaticGameObjects.playerLayer)
                            {
                                MyStaticGameObjects.playerKillCount++;
                                MyKillCountDisplay.instance.UpdateKillCountText();
                            }
                        }else if (target == null)
                        {
                            SetTarget(myCharacterStatsCache.gameObject);
                        }
                    }

                }
                ///<summary>Player's range weapon (archer)</summary>
                else if (collisonObject.layer == MyStaticGameObjects.companionRangeWeaponLayer)
                {
                    myCompanionRangeWeaponCache = collision.contacts[indexHit].otherCollider.GetComponent<MyCompanionRangeWeapon>();
                    if (myCompanionRangeWeaponCache.attacker.layer == MyStaticGameObjects.playerLayer && startHealth> 0)
                    {
                        if (myCompanionRangeWeaponCache.attacker.GetComponent<MyCharacterController>().proformAttack)
                        {
                            MyStaticGameObjects.arrowsHitTarget++;
                            myCompanionRangeWeaponCache.attacker.GetComponent<MyCharacterController>().proformAttack = false;
                        }

                    }
                    if (myCompanionRangeWeaponCache.arrowType == MyCompanionRangeWeapon.ArrowType.Lightning)
                    {
                        collision.contacts[indexHit].otherCollider.enabled = false;
                        arrowDistance = headTransform.position.y - collision.contacts[indexHit].point.y;
                      
                        //instant kill HeadShot
                        if (!isBoss && hp == GetMaxHP() && (arrowDistance < .8f && arrowDistance >-.8f) )
                        {
                            particle = MyObjectPools.headShotParticlePoolTransform.GetChild(MyObjectPools.instance.headShotParticlePool.GetMyObjectIndex()).gameObject;
                            particle.transform.position = collision.contacts[indexHit].point;
                            particle.GetComponent<ParticleSystem>().Play(true);
                            hp = 0;
                            MyStaticGameObjects.headShotCount++;

                            Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.headShotFX));
                        }
                        else if (!TakesDamage(myCompanionRangeWeaponCache.attackPoint))
                        {
                            CreateMissParticle(collision.contacts[indexHit].point);
                        }

                    }
                    else if (myCompanionRangeWeaponCache.arrowType == MyCompanionRangeWeapon.ArrowType.Magic)
                    {
                        collision.contacts[indexHit].otherCollider.enabled = false;
                        //slow arrow
                        ReduceMonsterSpeed();
                    }
                    else
                    {

                        collision.contacts[indexHit].otherCollider.enabled = false;
                        myCompanionRangeWeaponCache.CreateParticle(collision);
                        OnHitSound();
                    }

                    if (startHealth> 0 && hp < 1 && myCompanionRangeWeaponCache.attacker != null && !isBoss)
                    {
                        DeadMonster();
                        collider.enabled = false;
                        if (myCompanionRangeWeaponCache.attacker.layer == MyStaticGameObjects.playerLayer)
                        {
                            MyStaticGameObjects.playerKillCount++;
                            MyKillCountDisplay.instance.UpdateKillCountText();
                        }
                    }else if (target == null)
                    {
                        SetTarget(myCompanionRangeWeaponCache.attacker);
                    }
                   
                    collision.contacts[indexHit].otherCollider.gameObject.SetActive(false);
                }

                indexHit++;
                startHealth = hp;
            }
            lastColliderID = 0;
        }
       
        /// <summary>
        /// Push the monster toward the forceDirection by the force from the forceOrigin
        /// </summary>
        /// <param name="force"></param>
        /// <param name="forceOrigin"></param>
        /// <param name="forcetDirection"></param>
        protected internal void ApplyForce(float force, Vector3 forceOrigin,Vector3 forcetDirection)
        {
            rigidbody.isKinematic = false;
            rigidbody.AddForce(forcetDirection * force, ForceMode.Impulse);
            //rigidbody.AddRelativeForce(direction * force,ForceMode.Impulse) ;
            //rigidbody.AddExplosionForce(force, forceOrigin, 200, 2000.0f, ForceMode.Impulse);
            lastCollisionTime = Time.time;
            navMeshAgent.updatePosition = false;
          }

        /// <summary>
        /// Create onHit particle and start the animation at thePosition
        /// </summary>
        /// <param name="thePosition"></param>
        private void CreateOnHitParticle(Vector3 thePosition)
        {
            particle = MyObjectPools.onHitParticlePoolTransform.GetChild(MyObjectPools.instance.onHitParticlePool.GetMyObjectIndex()).gameObject;
            particle.transform.position = thePosition;
            particle.GetComponent<ParticleSystem>().Play(true);
        }
       
        /// <summary>
        /// Create miss particle and start the animation at thePosition
        /// </summary>
        /// <param name="thePosition"></param>
        private void CreateMissParticle(Vector3 thePosition)
        {
            particle = MyObjectPools.missedParticlePoolTransform.GetChild(MyObjectPools.instance.missedParticlePool.GetMyObjectIndex()).gameObject;
            particle.transform.position = thePosition;
            particle.GetComponent<ParticleSystem>().Play(true);
        }
       
        /// <summary>
        /// Set the monsters in death states if it is active and begin fade out the monster
        /// </summary>
        protected internal void DeadMonster()
        {
            if (isActive && !isDead)
            {
                targetCollider.enabled = false;
                if (isBoss)
                {
                    hp = 10;
                }
                else
                {
                    GivePlayersEXP();
                    DropItem();
                 
                }
                if (!startedMovementCoroutine)
                {
                    
                    DeathAnimation();
                    isPoison = false;
                    isSlowDown = false;
                    navMeshAgent.speed = defaultNavMeshSpeed;
                    animator.speed = defaultSpeed;
                    Timing.RunCoroutine(FadeObject());
                
                    target = null;
                    targetTransform = null;
                    targetCharacterStats = null;
                    isDead = true;
                }
            }
        }
       
        /// <summary>
        /// Slow down the monster's movement 
        /// </summary>
        protected internal void ReduceMonsterSpeed()
        {
            if (!isSlowDown)
            {
                isSlowDown = true;
                slowParticle = MyObjectPools.slowedStatusParticlePoolTransform.GetChild(MyObjectPools.instance.slowedStatusParticlePool.GetMyObjectIndex()).gameObject;
                if (!slowParticle.activeSelf)
                {
                    slowParticle.SetActive(true);
                }
                slowParticle.GetComponent<MyAttachableParticles>().StartFollow(gameObject, MyAttachableParticles.StatusType.Slow);
                slowParticle.GetComponent<ParticleSystem>().Stop(true);
                slowParticle.GetComponent<ParticleSystem>().Play(true);
              
                animator.speed = .5f;
                navMeshAgent.speed = navMeshAgent.speed / 2f;
                slowStartTime = Time.time;
            }
        }
       
        /// <summary>
        /// Take poison damage if the monster is poisoned by attackPoint amount
        /// </summary>
        /// <param name="attackPoint"></param>
        protected internal void TakesPosionDamage(int attackPoint)
        {
            if (!isPoison && isActive)
            {
                isPoison = true;
                posionDamageAmount = attackPoint;
                posionParticle = MyObjectPools.poisonStatusParticlePoolTransform.GetChild(MyObjectPools.instance.poisonStatusParticlePool.GetMyObjectIndex()).gameObject;
                if (!posionParticle.activeSelf)
                {
                    posionParticle.SetActive(true);
                }
                posionParticle.GetComponent<MyAttachableParticles>().StartFollow(gameObject, MyAttachableParticles.StatusType.Poison);
                posionParticle.GetComponent<ParticleSystem>().Stop(true);
                posionParticle.GetComponent<ParticleSystem>().Play(true);
                startHealth = hp;
                posionStartTime = Time.time;
            }else if(isPoison && !isActive)
            {
                StopPoisonDamage();
            }
        }
        
        ///<sumary>Create Gem Item</sumary>
        ///
        /// 
        private void DropItem()
        {
           
            if (randomNum % 5 == 0)
            {
                //2 out of 10 chance drop a gem
                particle = MyObjectPools.gemParticleTransform.GetChild(MyObjectPools.instance.gemParticlePool.GetMyObjectIndex()).gameObject;
                bagPosition = theTransform.position;
                bagPosition.y = bagPosition.y + .8f;
                particle.GetComponent<MyItems>().ShowItem(bagPosition, gemItems);
            }else if (randomNum % 2 == 0)
            {
                //5 out of 10 chance drop a bag
                UpdateQuest();
            }
        }
        
        /// <summary>
        /// Update the quest status of the player  
        /// </summary>
        private void UpdateQuest()
        {
            if (MyQuestManager.instance.GetCurrentQuest().started && !MyQuestManager.instance.GetCurrentQuest().isCompleted) {
                if (MyQuestManager.instance.GetCurrentQuest().questID == wolfQuestId && monsterType == MonsterType.Werewolf)
                {
                    particle = MyObjectPools.bagParticlePoolTransform.GetChild(MyObjectPools.instance.bagParticlePool.GetMyObjectIndex()).gameObject;
                    bagPosition = theTransform.position;
                    bagPosition.y = bagPosition.y + .8f;
                    particle.GetComponent<MyItems>().ShowItem(bagPosition, monsterItems);
                       
                }
                else if (MyQuestManager.instance.GetCurrentQuest().GetType() == typeof(MyBountyQuest))
                {
                    ((MyBountyQuest)MyQuestManager.instance.GetCurrentQuest()).UpdateQuest(monsterType);
                }

            }
        }
       
        /// <summary>
        /// Give each hero divided exp 
        /// </summary>
        private void GivePlayersEXP()
        {
            MyStaticGameObjects.instance.companionAndPlayerStats[0].GainExp(divideExp);
            MyStaticGameObjects.instance.companionAndPlayerStats[1].GainExp(divideExp);
            MyStaticGameObjects.instance.companionAndPlayerStats[2].GainExp(divideExp);
            MyStaticGameObjects.instance.companionAndPlayerStats[3].GainExp(divideExp);
        }
     
        /// <summary>
        /// Fade the monster out of the screen when it dies
        /// stop movement
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> FadeObject()
        {
           
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.updateUpAxis = false;
                navMeshAgent.enabled = false;

                if (!MyVillagerController.playerIsInArena)
                {
                    if (NavMesh.SamplePosition(theTransform.position + rayCastOffSet, out navMeshHit, 10, walkableAreaMask))
                    {
                        currentRot.y = theTransform.rotation.eulerAngles.y;
                        theTransform.position = navMeshHit.position;
                        theTransform.rotation = Quaternion.FromToRotation(theTransform.up, navMeshHit.normal) * Quaternion.Euler(currentRot);
                    }
                }
            }

            TurnOffWeaponAttack();
            collider.enabled = false;
            yield return Timing.WaitForSeconds(2f);

            if (MyStaticGameObjects.qualityLevel > 2)
            {
                for (int j = 0; j < skinnedMeshRenderers.Length; j++)
                {
                    for (int i = 0; i < skinnedMeshRenderers[j].materials.Length; i++)
                    {
                        StandardShaderUtils.ChangeRenderMode(ref skinnedMeshRenderers[j].materials[i], StandardShaderUtils.BlendMode.Fade);
                    }

                }

                alpha = 1f;
                fadeColor = defaultFadeOutColor;
                fadeColor.a = 1;
                while (alpha > 0)
                {
                    for (int j = 0; j < skinnedMeshRenderers.Length; j++)
                    {
                        for (int i = 0; i < skinnedMeshRenderers[j].materials.Length; i++)
                        {
                           skinnedMeshRenderers[j].materials[i].color = fadeColor;
                        }

                    }
                        yield return Timing.WaitForSeconds(.2f);

                    alpha = alpha - .05f;
                    fadeColor.a = alpha;
                }
                fadeColor.a = 0;

                for (int j = 0; j < skinnedMeshRenderers.Length; j++)
                {
                    for (int i = 0; i < skinnedMeshRenderers[j].materials.Length; i++)
                    {
                        skinnedMeshRenderers[j].materials[i].color = fadeColor;
                    }
                    skinnedMeshRenderers[j].enabled = false;
                }
            }
            else
            {
                DisableMeshRenderer();
            }
            meleeCollider.enabled = false;
            targetCollider.enabled = false;
            theTransform.position = storagePoolLocation;

            animator.speed = 5f;
            animator.SetBool("Die", false);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.ResetTrigger("TakeDamage");
            yield return Timing.WaitForSeconds(4f);
                
            Deactivate();
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
        }
     
        /// <summary>
        /// Deactive the monster, reset stats and move to the storage location on the map 
        /// </summary>
        protected internal void Deactivate()
        {
            if(skinMeshGameObject == null)
            {
                Init();
            }
            myMonsterFixedUpdate.enabled = false;

            animator.ResetTrigger("TakeDamage");
            animator.SetBool("Die", false);
            IdleAnimation();
            isDead = false;
            countDown = 0;
            isPoison = false;
            isSlowDown = false;
            navMeshAgent.speed = defaultNavMeshSpeed;
            animator.speed = defaultSpeed;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            target = null;
            targetCharacterStats = null;
            targetTransform = null;
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
            collider.enabled = false;
            navMeshAgent.enabled = false;
            navMeshAgent.areaMask = walkableAreaMask;
            theTransform.position = storagePoolLocation;

            if (blobShadowMeshRenderer != null)
            {
                blobShadowMeshRenderer.enabled = true;
            }
            if (isActive)
            {
                MySpawnPointProperties.numberOfMonsterSpawned--;
            }
            if(coroutineFindTarget.IsValid)
            {
                Timing.PauseCoroutines(coroutineFindTarget); 
            }
            if (coroutineMovement.IsValid)
            {
                Timing.PauseCoroutines(coroutineMovement);
            }
          
            isActive = false;
            pendingSpawn = false;
            skinMeshGameObject.layer = 31;
            animator.enabled = false;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Disabled the mesh renderer so it doesn't get render 
        /// </summary>
        protected internal void DisableMeshRenderer()
        {
            for (int j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                skinnedMeshRenderers[j].enabled = false;
            }
        }
       
        /// <summary>
        /// Get Monster ready for battle reseting the stats to full health and enable movement
        /// </summary>
        /// <param name="theLevel"></param>
        /// <param name="position"></param>
        /// <param name="playerTransform"></param>
        protected internal void ResetStats(int theLevel,ref  Vector3 position,ref Transform playerTransform)
        {
            gameObject.SetActive(true);
            if (blobShadowMeshRenderer!= null)
            {
                blobShadowMeshRenderer.enabled = true;
            }
            TurnOffWeaponAttack();
       
            navMeshAgent.speed = defaultNavMeshSpeed;
            animator.speed = defaultSpeed;
            navMeshAgent.updateUpAxis = true;
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = true;

            level = theLevel;
            LevelUp();
        
            animator.ResetTrigger("TakeDamage");
            isDead = false;
            countDown = 0;
            isPoison = false;
            isSlowDown = false;
            isTargetInMeleeRange = false;
            isTargetInRangeAttackRange = false;
            if (!isActive)
            {
                MySpawnPointProperties.numberOfMonsterSpawned++;
            }
            isActive = false;
            
            target = null;
            targetCharacterStats = null;
            targetTransform = null;
            theTransform.position = position;
            animator.enabled = true;
            rigidbody.detectCollisions = true;
            myMonsterFixedUpdate.enabled = true;
            collider.enabled = true;
            fadeColor = defaultFadeOutColor;
          
            for (int y = 0; y < skinnedMeshRenderers.Length; y++)
            {
                if (MyStaticGameObjects.qualityLevel > 2)
                {
                    for (int i = 0; i < skinnedMeshRenderers[y].materials.Length; i++)
                    {
                        StandardShaderUtils.ChangeRenderMode(ref skinnedMeshRenderers[y].materials[i], StandardShaderUtils.BlendMode.Opaque);
                        skinnedMeshRenderers[y].materials[i].color = fadeColor;
                    }
                }
                skinnedMeshRenderers[y].enabled = true;
            }
            theTransform.LookAt(playerTransform);
            skinMeshGameObject.layer = 16;
        }
      
        /// <summary>
        /// create weapontrail 
        /// </summary>
        protected internal override void InitWeaponTrail() {
            if (xWeaponTrailR != null)
            {
                xWeaponTrailR.MyInit();
            }
            if (xWeaponTrailL != null)
            {
                xWeaponTrailL.MyInit();
            }
        }
      
        /// <summary>
        /// If the monster is attack return true otherwise return false
        /// </summary>
        /// <returns></returns>
        public override bool IsWeaponAttacking()
        {
            if (hp < 1)
            {
                return false;
            }
            return isWeaponAttacking;
        }
       
        /// <summary>
        /// Set the attack states to true and turn on the weapon collider and weapon trail
        /// </summary>
        public override void TurnOnWeaponAttack()
        {
            weaponCollider.enabled = true;
            if (weaponCollider2 != null)
            {
                weaponCollider2.enabled = true;
            }
            isWeaponAttacking = true;
            if (xWeaponTrailR != null)
            {
                xWeaponTrailR.Activate();
            }
            if (xWeaponTrailL != null)
            {
                xWeaponTrailL.Activate();
            }

        }
      
        /// <summary>
        /// Set the attack states to false and turn off the weapon collider and weapon trail
        /// </summary>
        public override void TurnOffWeaponAttack()
        {
            weaponCollider.enabled = false;
            if (weaponCollider2 != null)
            {
                weaponCollider2.enabled = false;
            }
            isWeaponAttacking = false;
            if (xWeaponTrailR != null)
            {
                xWeaponTrailR.Deactivate();
            }
            if (xWeaponTrailL != null)
            {
                xWeaponTrailL.Deactivate();
            }

        }
        
        /// <summary>
        /// Play monster foot step sound
        /// </summary>
        private void PlayFootStepSound()
        {
            ufootStepAudioSource.clip = MyCrossFadeAudioMixer.instance.footSteps[footstepsAudioIndex];
            ufootStepAudioSource.loop = false;
            ufootStepAudioSource.Play();
            footstepsAudioIndex++;
            if(footstepsAudioIndex>= MyCrossFadeAudioMixer.instance.footSteps.Length)
            {
                footstepsAudioIndex = 0;
            }
        }
       
        /// <summary>
        /// play the on Hit sound
        /// </summary>
        protected internal void OnHitSound()
        {
            uonHitAudioSource.clip = MyCrossFadeAudioMixer.instance.onHit[onHitAudioIndex];
            uonHitAudioSource.loop = false;
            uonHitAudioSource.Play();
            onHitAudioIndex++;
            if(onHitAudioIndex>= MyCrossFadeAudioMixer.instance.onHit.Length)
            {
                onHitAudioIndex = 0;
            }
        }
       
        /// <summary>
        /// play left foot step sound call by animation event
        /// </summary>
        public override void FootL()
        {
            if (playFootStepSound)
            {
                PlayFootStepSound();
            }
        }
     
        /// <summary>
        /// play right foot step sound call by animation event
        /// </summary>
        public override void FootR()
        {
            if (playFootStepSound)
            {
                PlayFootStepSound();
            }
        }
       
        /// <summary>
        /// play on hit sound call by animation event
        /// </summary>
        public override void Hit()
        {
            if (isBoss) { 
                weaponAudioIndex = Random.Range(0, MyCrossFadeAudioMixer.instance.bossWeaponSound.Length);
                uweaponAudioSource.clip = MyCrossFadeAudioMixer.instance.bossWeaponSound[weaponAudioIndex];
                uweaponAudioSource.loop = false;
                uweaponAudioSource.Play() ;
            }
            else { 
                weaponAudioIndex = Random.Range(0, MyCrossFadeAudioMixer.instance.weaponSound.Length);
                uweaponAudioSource.clip = MyCrossFadeAudioMixer.instance.weaponSound[weaponAudioIndex];
                uweaponAudioSource.loop = false;
                uweaponAudioSource.Play();
            }
        }
       
        /// <summary>
        /// Create the Spell Ball Attack particle
        /// </summary>
        //Range Attacks Animation Events
        public void CreateParticle()
        {
        
            magicBallParticle = MyObjectPools.spellParticlePoolTransform.GetChild(MyObjectPools.instance.spellParticlePool.GetMyObjectIndex()).gameObject;
            myMonsterRangeWeapon = magicBallParticle.GetComponent<MyMonsterRangeWeapon>();
            myMonsterRangeWeapon.attackPoint = attackPoint;
            myMonsterRangeWeapon.attacker = gameObject;
            myMonsterRangeWeapon.PlayParticles();
            magicBallParticle.transform.position = particlePoint.transform.position;
            magicBallParticle.GetComponent<Rigidbody>().velocity = vectorZero;
            magicBallParticle.GetComponent<MySelfDestoryController>().ResetClock();
        }
       
        /// <summary>
        /// Move the spell ball particles forward 
        /// </summary>
        //Range Attacks Animation Events
        public void FireParticle()
        {
            if (magicBallParticle != null)
            {
                magicBallParticle.GetComponent<Rigidbody>().AddForce(theTransform.forward * 500);
            }
        }
      
    }
}