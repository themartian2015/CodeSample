using MEC;
using MiniLegend.CloudServices;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XftWeapon;
using static MiniLegend.MyStorageItems;


namespace MiniLegend.CharacterStats
{
    /// <summary>
    /// Base Class for MyCharacterController, MyCompanionNavMeshAgents and MyMonsterNavMeshAgent  
    /// Use for handling the character stats for the character, monster, and companions
    /// </summary>
    public class MyCharacterStats : MonoBehaviour
    {
        private static int MAXLEVEL = 25;
        public string displayName = "";
        public int baseExp = 50;
        public int baseHp = 100;
        public int baseAttackPoint = 10;
        public int baseDefensePoint = 5;

        public bool hasRegen = false;
        public bool isArcher = false;
        public bool hasAttackBuff = false;
        public bool hasDefenseBuff = false;
        public bool isHealer = false;

        public ParticleSystem attackBuffParticle;
        public ParticleSystem regenParticle;
        public ParticleSystem defenseBuffParticle;
        public ParticleSystem levelUpParticle;
        public GameObject currentWeapon;
        public NavMeshAgent navMeshAgent;
        public List<GameObject> weaponList = new List<GameObject>();
        public Sprite uiImage;
        public Sprite dialogImage;
        public MyAudioSource onHitAudioSource;
        public MyAudioSource footStepAudioSource;
        public MyAudioSource weaponAudioSource;
     
        public MyCharacterPanel myCharacterPanel;
        public GameObject knockDownArea;
        private Quaternion particleRotation90;
        private Vector3 particleRotation = Vector3.zero;

        protected GameObject particle;
        protected Animator animator;
        protected Collider myWeaponCollider;
        protected Collider collider;
        protected Color fadeColor = Color.black;
        protected GameObject target;
        protected Rigidbody rigidbody;
        protected SkinnedMeshRenderer skinnedMeshRenderer;
        protected Transform theTransform;
        protected MyWeapons myWeapons;
        protected MyPlayerStats[] playerStats;
        protected internal MyCompanionRangeWeapon.ArrowType arrowType;
        protected internal ItemType weaponType;

        protected bool resetingTimeout = false;
        protected bool inDeathTimeout = false;
        protected bool revevived = false;
        protected bool hit = false;
        protected bool isWeaponAttacking = false;
        protected bool isInitialized = false;
        protected int damage = 0;
        protected int hp = 100;
        protected int attackPoint = 10;
        protected int defensePoint = 5;
        protected int level = 0;
        protected float changeInYAxis = 0;
        protected float previousYRotation = 0;
        protected Transform myBlobShadow = null;
        protected int healAmount = 0;
        protected float regenStartTime = 0;
        protected float attackBuffStartTime = 0;
        protected float defenseBuffStartTime = 0;
        protected float oneSecondInterval = 0;
        protected Transform knockDownTransform;
        protected Collider knockDownCollider;

        private int nextLevelMultipler = 3;
        private int previouslevelExp = 0;
        private int exp = 0;
        private int nextLevelExp = 0;
        private int maxHp = 0;
        private int regenCountDown = 30, attackBuffCountDown = 30, defenseBuffCountDown = 30;
        private int attackBuffAmount = 0, defenseBuffAmount = 0;
        private int tempDefensePoint = 0;
        private int tempAttackPoint = 0;
        private int previousAnimationState = -1;
        private float deathTimeout = 5f;
        private bool transition = false;
        private bool isInAnimation = false;
        private bool knockDownActive = false;
        private float knockDownStartTime = 0;

        private AnimatorStateInfo currentBaseLayerStateInfo;
        private AnimatorStateInfo nextAnimatorStateInfo;
        private GameObject myWeaponLightSource;
        private MyBowController myBowController;
        private NavMeshHit navMeshHit;
        private float tempHeight = 0;
        private float currentHeight = 0;
        private Vector3 heightPosition = Vector3.zero;
        private float lastBlobShadowHeightCalculationTime = 0;
        private static Vector3 shadowOffSet = new Vector3(0, .06f, 0);
        public MyAudioSource landingImpactAudioSource;
        private string PlayerAxemenStr = "PlayerAxemen";
        private string AxemenCompanionStr = "AxemenCompanion";
     
        public struct MyPlayerStats
        {
            public MyPlayerStats(int theHp, int theAttackPoint, int theDefensePoint)
            {
                hp = theHp;
                attackPoint = theAttackPoint;
                defensePoint = theDefensePoint;
            }
            public int hp;
            public int attackPoint;
            public int defensePoint;
        }

        /// <summary>
        /// Initizialize 
        /// </summary>
        protected virtual void Init()
        {
            if (!isInitialized)
            {
                if (navMeshAgent == null)
                {
                    navMeshAgent = GetComponent<NavMeshAgent>();
                }
                rigidbody = GetComponent<Rigidbody>();
                animator = GetComponent<Animator>();
                theTransform = GetComponent<Transform>();
                isInitialized = true;
                myBlobShadow = MyStaticGameObjects.CreateBlobShadow(theTransform);
                particleRotation90 = Quaternion.Euler(new Vector3(90, 0, 0));
                collider = GetComponent<Collider>();
            }
        }

        /// <summary>
        /// Calculate the postion of the blobShadow using the navmesh 
        /// </summary>
        protected internal void CalculateBlogShadowHeight()
        {
            if(myBlobShadow!= null)
            {
                if (MyBlobShadow.dynamicHeight && Time.time - lastBlobShadowHeightCalculationTime > .3f  &&  theTransform.position.y > -500  )
                {
                    tempHeight = myBlobShadow.position.y - currentHeight;
                    if (tempHeight > .03f || tempHeight < -.03f)
                    {
                         if (NavMesh.SamplePosition(theTransform.position, out navMeshHit, 2, NavMesh.AllAreas))
                         {
                             heightPosition = navMeshHit.position + shadowOffSet;
                             heightPosition.x = myBlobShadow.position.x;
                             heightPosition.z = myBlobShadow.position.z;
                             myBlobShadow.position = heightPosition;
                             currentHeight = myBlobShadow.position.y;
                         }
                        //heightPosition = theTransform.position + shadowOffSet;

                        myBlobShadow.position = heightPosition;
                        currentHeight = myBlobShadow.position.y;
                    }
                    lastBlobShadowHeightCalculationTime = Time.time;
                }
            }
        }
        
        /// <summary>
        /// Get the Current Attack Target use the companion and monsters
        /// </summary>
        /// <returns></returns>
        protected internal GameObject GetTarget()
        {
            return target;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal bool isInDeathTimeout()
        {
            return inDeathTimeout;
        }

        /// <summary>
        /// Flash the character 
        /// </summary>
        /// <returns></returns>
        private IEnumerator<float> Blink()
        {
            bool faded = false;
            StandardShaderUtils.ChangeRenderMode(ref characterMaterial, StandardShaderUtils.BlendMode.Fade);
            fadeColor.a = 1f;
            fadeColor.r = 0f;
            fadeColor.g = 0f;
            fadeColor.b = 0f;

            if (MyStaticGameObjects.qualityLevel > 2)
            {
                while (revevived)
                {
                    skinnedMeshRenderer.materials[0].SetColor("_EmissionColor", fadeColor);
                    skinnedMeshRenderer.materials[0].EnableKeyword("_EMISSION");
                    if (skinnedMeshRenderer.materials.Length > 1)
                    {
                        skinnedMeshRenderer.materials[1].SetColor("_EmissionColor", fadeColor);
                        skinnedMeshRenderer.materials[1].EnableKeyword("_EMISSION");
                    }
                    if (!faded)
                    {
                        fadeColor.r = fadeColor.r - .1f;
                        fadeColor.g = fadeColor.g - .1f;
                        fadeColor.b = fadeColor.b - .1f;
                        //fadeColor.a = fadeColor.a - .1f;
                    }
                    else
                    {
                        fadeColor.r = fadeColor.r + .1f;
                        fadeColor.g = fadeColor.g + .1f;
                        fadeColor.b = fadeColor.b + .1f;
                    }
                    if (fadeColor.g <= 0)
                    {
                        faded = true;
                    }
                    else if (fadeColor.g >= 1)
                    {
                        faded = false;
                    }

                    yield return Timing.WaitForOneFrame;
                }
                fadeColor.r = 0f;
                fadeColor.g = 0f;
                fadeColor.b = 0f;
                skinnedMeshRenderer.materials[0].SetColor("_EmissionColor", fadeColor);
                skinnedMeshRenderer.materials[0].DisableKeyword("_EMISSION");
                if (skinnedMeshRenderer.materials.Length > 1)
                {
                    skinnedMeshRenderer.materials[1].SetColor("_EmissionColor", fadeColor);
                    skinnedMeshRenderer.materials[1].DisableKeyword("_EMISSION");
                }
            }
            else
            {
                yield return Timing.WaitForOneFrame;
            }
             StandardShaderUtils.ChangeRenderMode(ref characterMaterial, StandardShaderUtils.BlendMode.Opaque);
        }

        /// <summary>
        /// Calls every second to handle the Regen,and character Buff
        /// </summary>
        protected virtual internal void OneSecondInterval()
        {

            //one second interval
            if (Time.time - oneSecondInterval > 1)
            {
                oneSecondInterval = Time.time;

                if (hasRegen)
                {
                    //within 30 second lnterval
                    if (Time.time - regenStartTime < 30 && hp > 0)
                    {
                        if (hp < GetMaxHP())
                        {
                           // Debug.Log("Healing:" + healAmount);
                            hp = hp + healAmount;
                        }
                        if (hp > GetMaxHP())
                        {
                            hp = GetMaxHP();
                        }
                    }
                    else
                    {
                        //Stop Regen
                        hasRegen = false;
                        regenParticle.Stop(true);
                        healAmount = 0;
                    }

                }
                if (hasAttackBuff)
                {
                    //Stop the attack  buff after 30 seconds or character is dead
                    if (Time.time - attackBuffStartTime > 30 && hp < 0)
                    { 
                        //Stop attack buff
                        attackPoint = playerStats[level].attackPoint;
                        attackBuffParticle.Stop(true);
                        hasAttackBuff = false;
                        attackBuffAmount = 0;
                    }
                }

                if (hasDefenseBuff)
                {
                    //Stop the defense buff after 30 seconds or character is dead
                    if (Time.time - defenseBuffStartTime > 30 && hp < 0)
                    {
                        //Stop defense buff
                        defensePoint = playerStats[level].defensePoint;
                        defenseBuffParticle.Stop(true);
                        hasDefenseBuff = false;
                        defenseBuffAmount = 0;
                    }
                }
                UpdateHealthBar();

            }
            if(knockDownActive && Time.time - knockDownStartTime > .3f)
            {
                if(knockDownArea!= null)
                {
                    knockDownCollider.enabled = false;
                }
                knockDownActive = false;
            }
        }

        /// <summary>
        /// Reset the player stats Based on the Character level
        /// </summary>
        protected internal void ResetBuffStats()
        {
            defensePoint = playerStats[level].defensePoint;
            defenseBuffParticle.Stop(true);
            hasDefenseBuff = false;
            defenseBuffAmount = 0;
            attackPoint = playerStats[level].attackPoint;
            attackBuffParticle.Stop(true);
            hasAttackBuff = false;
            attackBuffAmount = 0;
            hasRegen = false;
            regenParticle.Stop(true);
            healAmount = 0;
            UpdateHealthBar();
        }

        /// <summary>
        /// Start a 10 second Reviewd Protectopm
        /// </summary>
        protected internal void RevivedProtection()
        {
            if (!revevived)
            {
                hp = GetMaxHP();
                UpdateHealthBar();
                revevived = true;
                Timing.RunCoroutine(Blink());
                Timing.RunCoroutine(StartReviveProtection());
            }
        }
        /// <summary>
        /// 10 Count down for the character Revived 
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> StartReviveProtection()
        {
            yield return Timing.WaitForSeconds(10f);
            revevived = false;
        }
        
        /// <summary>
        /// Add Health based on gain Hp
        /// </summary>
        /// <param name="gainHP"></param>
        protected internal  void GiveHealth(int gainHP)
        {
            hp = hp + gainHP;
            if (hp > maxHp)
            {
                hp = maxHp;
            }
            UpdateHealthBar();
        }

        /// <summary>
        /// Reset the character Level and stats
        /// </summary>
        protected internal void ResetLevel()
        {
            exp = 0;
            level = 0;
            hp = 1;
            LevelUp();
            UpdateHealthBar();
        }

        /// <summary>
        ///  Return current Attack target not use by the player(main Character)
        /// </summary>
        /// <param name="theTarget"></param>
        protected internal virtual void SetTarget(GameObject theTarget)
        {
            target = theTarget;
        }

        /// <summary>
        /// Returns Current Hp
        /// </summary>
        /// <returns></returns>
        public int GetHp()
        {
            return hp;
        }

        /// <summary>
        /// Return current level
        /// </summary>
        /// <returns></returns>
        public int GetLevel()
        {
            return level;
        }

       
        /// <summary>
        /// Update the text for the health Bar
        /// </summary>
        public void UpdateHealthBar()
        {
            if (myCharacterPanel != null)
            {
                myCharacterPanel.UpdateHealthAndStatusIcon(GetHPDecemial(), GetEXPDecemial(), level, hasRegen, hasAttackBuff, hasDefenseBuff);
            }
        }

        /// <summary>
        ///  Start Health Regen for 30 seconds
        /// </summary>
        /// <param name="amount"></param>
        public void StartRegen(int amount)
        {
            if (!hasRegen)
            {
                hasRegen = true;
                regenParticle.Play(true);
                regenStartTime = Time.time;
                healAmount = amount;
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Stat Attack buff for 30 second
        /// </summary>
        /// <param name="amount"></param>
        public void StartAttackBuff(int amount)
        {
            if (!hasAttackBuff)
            {
                hasAttackBuff = true;
                attackBuffParticle.Play(true);
                attackBuffStartTime = Time.time;
                attackBuffAmount = amount;
                attackPoint = playerStats[level].attackPoint + amount;
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Start Defense buff for 30 seconds
        /// </summary>
        /// <param name="amount"></param>
        public void StartDefenseBuff(int amount)
        {
            if (!hasDefenseBuff)
            {
                hasDefenseBuff = true;
                defenseBuffParticle.Play(true);
                defenseBuffStartTime = Time.time;
                defenseBuffAmount = amount;
                defensePoint = playerStats[level].defensePoint + amount;
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Switch Character weapon only use by companion and the player
        /// </summary>
        /// <param name="itemId"></param>
        public void SwitchWeapon(ItemsId itemId)
        {
            bool isLightOn = false;

            if (currentWeapon != null )
            {
                if (myWeaponLightSource != null)
                {
                    isLightOn = myWeaponLightSource.GetComponent<Light>().enabled;
                    TurnOffWeaponLight();
                }
                currentWeapon.SetActive(false);
            }
            Debug.Log("weaponList  count" + weaponList.Count);
            for (int z = 0; z < weaponList.Count; z++)
            {
                if (weaponList[z].GetComponent<MyWeapons>().weaponId == itemId)
                {
                    currentWeapon = weaponList[z];
                    myWeapons = weaponList[z].GetComponent<MyWeapons>();
                    currentWeapon.SetActive(true);
                    if (currentWeapon.GetComponent<Collider>() != null)
                    {
                        myWeaponCollider = currentWeapon.GetComponent<Collider>();
                    }

                    if (myWeapons.weaponLight != null)
                    {
                        myWeaponLightSource = myWeapons.weaponLight;
                        if (isLightOn)
                        {
                            TurnOnWeaponLight();
                        }
                        else
                        {
                            TurnOffWeaponLight();
                        }
                    }

                    if (myBowController != null)
                    {
                        myBowController.theBowAnimator = myWeapons.theAnimator;
                    }
                    currentWeapon.SetActive(true);
                    break;
                }
            }
        }


       /// <summary>
       ///  turn on the weapon light for the preist
       /// </summary>
        public void TurnOnWeaponLight()
        {
            if (myWeaponLightSource != null )
            {
                //myWeaponLightSource.GetComponent<Light>().intensity = MyStaticGameObjects.weaponLightIntensity;
                //myWeaponLightSource.GetComponent<Light>().range = MyStaticGameObjects.weaponLightRange;
                myWeaponLightSource.GetComponent<Light>().enabled = true;
            }
        }

        /// <summary>
        /// Turn off the weapon light for the preist
        /// </summary>
        public void TurnOffWeaponLight()
        {
            if (myWeaponLightSource != null )
            {
                //myWeaponLightSource.GetComponent<Light>().intensity = 0;
               // myWeaponLightSource.GetComponent<Light>().range = 0;
                myWeaponLightSource.GetComponent<Light>().enabled = false;

            }
        }

        /// <summary>
        /// Add Exp and LevelUp the character
        /// </summary>
        /// <param name="theExp"></param>
        protected internal void GainExp(int theExp)
        {
            if (level < MAXLEVEL)
            {
                // Debug.Log(name+ " companionnextLevelExp: " + nextLevelExp + " GainExp: " + theExp + " exp:" + exp);
                exp = exp + theExp;
                if (exp >= nextLevelExp)
                {
                    if (hp > 0)
                    {
                        if (levelUpParticle == null)
                        {
                            Debug.Log("levelup particle is null " + gameObject.name);
                        }
                        else
                        {

                            levelUpParticle.Play(true);
                            if (level != 1)
                            {
                                Timing.RunCoroutine(MyCrossFadeAudioMixer.instance.PlaySoundOnce(1f, MyCrossFadeAudioMixer.instance.levelUpFx));
                            }
                        }
                    }
                    LevelUp();
                }
            }
            else
            {
                exp = nextLevelExp;
            }
        }

        /// <summary>
        /// Gain a Level and update the character stats based on precalculated statss in playerStats
        /// </summary>
        protected virtual void LevelUp()
        {
            level = level + 1;
            maxHp = playerStats[level].hp;
            if (MyStaticGameObjects.gainExpWhenDead || hp > 0)
            {
                hp = maxHp;// baseHp + (int)Math.Exp(level);
            }

            UpdateHealthBar();
            defensePoint = playerStats[level].defensePoint + defenseBuffAmount;
            attackPoint = playerStats[level].attackPoint + attackBuffAmount;
            previouslevelExp = nextLevelExp;
            nextLevelExp = GetNextLevelExp(level);
        }

        /// <summary>
        /// PreCalculate the character stats up to level 25
        /// </summary>
        protected void CalculateStats()
        {
            int calMaxHP = 0;
            int calDefensePoint = 0;
            int calAttackPoint = 0;

            for (int x = 0; x < 26; x++)
            {
                calMaxHP = baseHp + MyStaticGameObjects.instance.GetMyLevelUpEquation(x);
                calDefensePoint = baseDefensePoint + MyStaticGameObjects.instance.GetMyLevelUpEquation(x);
                calAttackPoint = baseAttackPoint + MyStaticGameObjects.instance.GetMyLevelUpEquation(x);
                playerStats[x].attackPoint = calAttackPoint;
                playerStats[x].defensePoint = calDefensePoint;
                playerStats[x].hp = calMaxHP;
                //Debug.Log("PlayerStatsCompanion: level " + x + " calAttackPoint:" + calAttackPoint + " DefensePoint: " + calDefensePoint + " hp: " + calMaxHP);

            }
        }

        /// <summary>
        /// Get the exp require to Level UP 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private int GetNextLevelExp(int level)
        {
            if (level % 5 == 0)
            {
                nextLevelMultipler++;
            }
            return exp + baseExp + MyStaticGameObjects.instance.GetMyLevelUpEquation(level) * level * nextLevelMultipler;
        }

        /// <summary>
        /// Get the Character Attack point base on the current level
        /// </summary>
        /// <param name="includeBoost"></param>
        /// <returns></returns>
        public int GetAttackPoint(bool includeBoost = true)
        {
            if (myWeapons != null)
            {
                tempAttackPoint = attackPoint + myWeapons.attackPoint;

                if (includeBoost)
                {
                    if (MyGameService.instance != null)
                    {
                        if (MyGameService.instance.GetLastAttackBoostTimeSpan().TotalMinutes < 30)
                        {
                            tempAttackPoint = attackPoint + myWeapons.attackPoint + ((int)(attackPoint * .1f) * MyGameService.instance.attackBoostStack);
                        }
                    }
                }
                return tempAttackPoint;
            }
            else
            {
                return attackPoint;
            }
        }

        /// <summary>
        /// Get the player Defenese point based on the character level 
        /// </summary>
        /// <param name="includeBoost"></param>
        /// <returns></returns>
        public int GetDefensePoint(bool includeBoost = true)
        {
            tempDefensePoint = defensePoint;
            if (includeBoost)
            {
                if (MyGameService.instance != null)
                {
                    if (MyGameService.instance.GetLastDefenseBoostTimeSpan().TotalMinutes < 30)
                    {
                        tempDefensePoint = defensePoint + ((int)(defensePoint * .1f) * MyGameService.instance.defenseBoostStack);
                    }
                }
            }
            return tempDefensePoint;
        }

        /// <summary>
        /// Get the MaxHp Based on the current level
        /// </summary>
        /// <returns></returns>
        public virtual int GetMaxHP()
        {
            return maxHp;
        }

        /// <summary>
        /// Get the CurrentExp Divide by the exp require to Level in decemial 
        /// </summary>
        /// <returns></returns>
        public float GetEXPDecemial()
        {
            if (level == 1)
            {
                return (float)exp / (float)nextLevelExp;
            }
            else
            {
                return ((float)(exp - previouslevelExp) / ((float)nextLevelExp - previouslevelExp));
            }
        }

        /// <summary>
        /// Get the current HP divide by the Max Hp for the current level
        /// </summary>
        /// <returns></returns>
        public float GetHPDecemial()
        {
            //Debug.Log(GetMaxHP());
            return (float)hp / (float)GetMaxHP();
        }

        /// <summary>
        /// Set a 5 second dealth timeout
        /// </summary>
        /// <returns></returns>
        protected IEnumerator<float> ResetDeathTimeOut()
        {
            inDeathTimeout = true;
            yield return Timing.WaitForSeconds(5f);
            inDeathTimeout = false;
            //resetingTimeout = false;
        }

        /// <summary>
        /// Take damge base on damegePoint if greater than defense
        /// </summary>
        /// <param name="damagePoint"></param>
        /// <returns></returns>
        protected internal virtual bool TakesDamage(int damagePoint)
        {
            hit = false;
            if (!revevived)
            {
                damage = defensePoint - damagePoint;
                if (damage < 0)
                {
                    hp = hp + damage;
                }
                else
                {
                    hit = true;
                }
                if (hp < 0)
                {
                    hp = 0;
                }

                UpdateHealthBar();
            }
            return hit;
        }

        /// <summary>
        /// Find all the character weapons for the player and companions
        /// </summary>
        public void FindCharacterWeapons()
        {
            MyWeapons[] myWeapons = GetComponentsInChildren<MyWeapons>(true);
            MyStaticGameObjects.instance.weaponCache.AddRange(myWeapons);

            if (isHealer)
            {
                Light[] weaponLight = GetComponentsInChildren<Light>(true);
                if (MyStaticGameObjects.instance.areaObjectTransition != null) {

                    for (int x = 0; x < weaponLight.Length; x++) {
                        //MyStaticGameObjects.instance.weaponCache
                        if (MyStaticGameObjects.instance.areaObjectTransition.transform.Find("CaveWeaponLightDeactivate") != null)
                        {
                            MyStaticGameObjects.instance.areaObjectTransition.transform.Find("CaveWeaponLightDeactivate").GetComponent<MyManualAreaCull>().deactivateList.Add(weaponLight[x].gameObject);
                        }
                        if (MyStaticGameObjects.instance.areaObjectTransition.transform.Find("CaveWeaponLightActivate") != null)
                        {
                            MyStaticGameObjects.instance.areaObjectTransition.transform.Find("CaveWeaponLightActivate").GetComponent<MyManualAreaCull>().activateList.Add(weaponLight[x].gameObject);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if the Character is in aninmation 
        /// </summary>
        /// <param name="animationStates"></param>
        /// <returns></returns>
        public bool IsInAnimation(int animationStates)
        {
            isInAnimation = false;
            currentBaseLayerStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            previousAnimationState = currentBaseLayerStateInfo.fullPathHash;
            if (previousAnimationState == animationStates)
            {
                isInAnimation = true;
            }

            return isInAnimation;
        }

        /// <summary>
        /// Check if the Character is in any of the aninmation in the list  
        /// </summary>
        /// <param name="animationStates"></param>
        /// <returns></returns>
        public bool IsInAnimations(int[] animationStates)
        {
            isInAnimation = false;
            currentBaseLayerStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            previousAnimationState = currentBaseLayerStateInfo.fullPathHash;
            for (int x = 0; x < animationStates.Length; x++)
            {
                if (previousAnimationState == animationStates[x])
                {
                    isInAnimation = true;
                    break;
                }
            }

            return isInAnimation;
        }

        /// <summary>
        /// Check if the character is in Animation transition
        /// </summary>
        /// <returns></returns>
        public bool IsInTransition()
        {
            transition = false;
            nextAnimatorStateInfo = animator.GetNextAnimatorStateInfo(0);
            currentBaseLayerStateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (animator.IsInTransition(0))
            {
                if (nextAnimatorStateInfo.fullPathHash == MyAnimationStates.idleState
                    || previousAnimationState == MyAnimationStates.idleState
                    || previousYRotation == MyAnimationStates.dieState
                    || nextAnimatorStateInfo.fullPathHash == MyAnimationStates.runState)
                {
                    transition = true;
                }
            }
            return transition;
        }

        /// <summary>
        /// Creates a  weapon trail for the warrior, axemen and monsters
        /// </summary>
        protected internal virtual void InitWeaponTrail()
        {
            if (myWeapons != null && myWeapons.xWeaponTrail != null)
            {
                myWeapons.xWeaponTrail.MyInit();
                myWeapons.xWeaponTrail.ResetCamera();
            }
        }

        /// <summary>
        /// Return true if character is attacking
        /// </summary>
        /// <returns></returns>
        public virtual bool IsWeaponAttacking()
        {
            return isWeaponAttacking;
        }

        /// <summary>
        /// Begin attack and weapon trail and collider
        /// </summary>
        public virtual void TurnOnWeaponAttack()
        {
            isWeaponAttacking = true;
            if (myWeapons != null && myWeapons.xWeaponTrail!= null)
            {
                myWeapons.xWeaponTrail.Activate();
            }
            if (myWeaponCollider != null)
            {
                myWeaponCollider.enabled = true;
            }
        }

        /// <summary>
        /// Stop attack and weapon trail and collider
        /// </summary>
        public virtual void TurnOffWeaponAttack()
        {
            isWeaponAttacking = false;
            if (myWeapons != null && myWeapons.xWeaponTrail!= null)
            {
                myWeapons.xWeaponTrail.Deactivate();
            }
            if (myWeaponCollider != null)
            {
                myWeaponCollider.enabled = false;
            }
        }

        #region AnimationEvents
        /// <summary>
        /// Play sound for left foot touch ground 
        /// </summary>
        public virtual void FootL()
        {
            footStepAudioSource.Play();
        }

        /// <summary>
        /// Play sound for right foot touch ground 
        /// </summary>
        public virtual void FootR()
        {
            footStepAudioSource.Play();
        }

        /// <summary>
        /// Create VFX for attack animation
        /// </summary>
        public void SpinParticle()
        {
           
                particle = MyObjectPools.spinParticlePoolTransform.GetChild(MyObjectPools.spinParticlePool.GetMyObjectIndex()).gameObject;
                particle.GetComponent<ParticleSystem>().Play(true);
                particle.transform.position = transform.position;
                particle.transform.rotation = particleRotation90;
            
        }
      

        /// <summary>
        /// Create VFX for attack animation
        /// </summary>
        public void CounterSpinParticle()
        {
            particle = MyObjectPools.counterSpinParticlePoolTransform.GetChild(MyObjectPools.counterSpinParticlePool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;

            particleRotation.y = theTransform.rotation.eulerAngles.y + 90f;
            particleRotation.z = 0;
            if (name == PlayerAxemenStr)
            {
                particleRotation.x = -45;
                particle.transform.rotation = Quaternion.Euler(particleRotation);
            }
            else
            {
                particleRotation.x = 45;
                particle.transform.rotation = Quaternion.Euler(particleRotation);
            }
            //collider.enabled = false;
            
        }

        /// <summary>
        /// Create VFX for attack animation
        /// </summary>
        public void VerticleSpinParticle()
        {
          
            particle = MyObjectPools.verticleSpinParticlePoolTransform.GetChild(MyObjectPools.verticleSpinParticlePool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;
            particleRotation.x = 0;
            particleRotation.y = transform.rotation.eulerAngles.y + 90f;
            particleRotation.z = 0;

            particle.transform.rotation = Quaternion.Euler(particleRotation);
            
        }

        /// <summary>
        /// Create VFX for landing animation
        /// </summary>
        public void LandImpactDust()
        {
           
            particle = MyObjectPools.landParticleDustPoolTransform.GetChild(MyObjectPools.landParticleDustPool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;
           
        }

        /// <summary>
        /// Create Vfx and SFX for knockdown efffects
        /// </summary>
        public void LandImpact()
        {
           
            particle = MyObjectPools.landParticleDustPoolTransform.GetChild(MyObjectPools.landParticleDustPool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;
            
          
            particle = MyObjectPools.landParticlePoolTransform.GetChild(MyObjectPools.landParticlePool.GetMyObjectIndex()).gameObject;
            particle.GetComponent<ParticleSystem>().Play(true);
            particle.transform.position = theTransform.position;
            
            if (knockDownArea != null)
            {
                knockDownStartTime = Time.time;
                knockDownActive = true;
                knockDownCollider.enabled = true;
                knockDownTransform.position = theTransform.position;
             
            }
            if (landingImpactAudioSource != null)
            {
                landingImpactAudioSource.Play();
            }
        }

    
        public virtual void Hit(){ }
        public virtual void AttackVoice(){ }
        public virtual void SpinningFXOff(){ }
        public virtual void SpinningFXOn(int loop = 0) {}
        public void Shoot() { }
        public void TargetPlayer() { }
        public virtual void HealPlayers() { }

        #endregion
    }

}