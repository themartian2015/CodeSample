using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XftWeapon;
using MEC;
using static MiniLegend.MyStorageItems;

namespace MiniLegend.CharacterStats
{
    /// <summary>
    /// This Class controls the Companion movement, animations and character stats 
    /// </summary>
    public class MyCompanionNavMeshAgent : MyCharacterStats
    {
        
        public int playerFollowStopDistance = 5;
        public float monsterAttackStopDistance = 1.8f;
        public float monsterAttackStopCompareDistance = 2;
        public bool isMonsterInRange = false;
        public bool hasRangeAttack = false;

        public MyCompanionAttackRange myCompanionAttackRange; 
 
        private float healingDistance = 20f;
        private int index = 0;
        private float yAxisDistance = 0;
      
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 particleOffset = new Vector3(0, 1, 0);
        private Vector3 previousDestination = Vector3.zero;
      
        private GameObject other;
        private MyMonsterNavMeshAgent myMonsterNavMeshAgentCached;
        private MyMonsterWeapon myMonsterWeaponCached;
        private MyMonsterRangeWeapon monsterRangeWeaponCached;
        private MyCharacterStats myCharacterStatsCached;
        private MyCharacterStats myCharacterControllerHealingCached;
        private MyCharacterStats myhealingCharacterStatsCached;
        private GameObject healingGameObject;
       
        private float distanceFromTarget = 100f;
        private bool inAttackRange = false;
        private Vector3 zeroVector = Vector3.zero;
        private bool idleState = false;
        private static int[] stationaryStates;
        NavMeshHit navMeshHit;
       
        private Vector3 rayCastOffSet = new Vector3(0, 1, 0);

        private bool isDead = false;
        private Transform targetTransform;
        private MyCharacterStats targetMyCharacterStats;
        private Transform playerTransform;

        private Vector2 companionVector2 = Vector2.zero;
        private Vector2 targetVector2 = Vector2.zero;
        private float maxDistance = 25;
        private float navMeshControlInterval = 0.25f;
        public IEnumerator<float> ResetTargetList()
        {
            if (!isHealer)
            {
                yield return Timing.WaitUntilDone(Timing.RunCoroutine(myCompanionAttackRange.ResetTargetList()));
                SetTarget(null);
            }
            yield return 0;
        }
     
        // Use this for initialization
        protected internal void Start()
        {
            Init();
        }
        protected override void Init()
        {
            if (isInitialized)
                return;

            navMeshControlInterval = UnityEngine.Random.Range(.24f, .27f);
            monsterAttackStopCompareDistance = monsterAttackStopDistance * monsterAttackStopDistance;
            healingDistance = healingDistance * healingDistance;
            maxDistance = maxDistance * maxDistance + 1;

            playerTransform = MyStaticGameObjects.instance.player.transform;
            playerStats = new MyPlayerStats[26];
            CalculateStats();
            base.Init();
            stationaryStates = new int[] { MyAnimationStates.reviveState, MyAnimationStates.spellEndState, MyAnimationStates.spellLoopState, MyAnimationStates.attackState, MyAnimationStates.attack2State, MyAnimationStates.attack3State, MyAnimationStates.attack4State, MyAnimationStates.dieState };

       
            target = null;
            myWeapons = currentWeapon.GetComponent<MyWeapons>();
            myWeaponCollider = currentWeapon.GetComponent<Collider>();
            weaponType = myWeapons.itemType;
            SwitchWeapon(myWeapons.weaponId);
            if (MyStaticGameObjects.qualityLevel > 2)
            {
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
            InitWeaponTrail();
            TurnOffWeaponAttack();
            TurnOffWeaponLight();
            previousYRotation = theTransform.rotation.eulerAngles.y;

            //Set a debug level for the character at the start of the game 
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
           
        }
        /// <summary>
        /// When the character enables we call a method every .25 second instead of using the Update function for every frame
        /// </summary>
        public void OnEnable()
        {
            Timing.RunCoroutine(NavMeshControl());
        }
      
        /// <summary>
        /// Called every .25 second to control the character movements and behaviors
        /// </summary>
        /// <returns></returns>
        IEnumerator<float> NavMeshControl()
        {
            if(theTransform== null)
            {
                theTransform = GetComponent<Transform>();
            }
            while (isActiveAndEnabled)
            {
                if (navMeshAgent != null && navMeshAgent.enabled)
                {
                    if (isHealer && !MyCharacterController.isEscaping)
                    {
                        FindHealingTarget();
                    }
                    TargetAndMovement();
                }
                yield return Timing.WaitForSeconds(navMeshControlInterval);
            }
        }

        /// <summary>
        /// if alive find target and attack or follow the main character if no attack target 
        /// if priest will find a companion to heal or follow the main character
        /// </summary>
        void TargetAndMovement()
        {
            if (hp <= 0)
            {
                ResetBuffStats();
                DeathAnimation();
                target = null;
                targetTransform = null;
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;

                if (!resetingTimeout)
                {
                    resetingTimeout = true;
                    Timing.RunCoroutine(ResetDeathTimeOut());
                }
                return;
            }
            else
            {
                OneSecondInterval();
                CalculateBlogShadowHeight();
                isDead = false;
                inAttackRange = false;
             
                resetingTimeout = false;
            

                if (navMeshAgent.enabled) { 
                    if (MyCharacterController.isEscaping)
                    {
                        inAttackRange = false;
                        target = null;
                        targetTransform = null;
                    }
                    else
                    {
                        if (navMeshAgent.isOnNavMesh && target != null)
                        {
                            targetVector2.x = targetTransform.position.x;
                            targetVector2.y = targetTransform.position.z;

                            companionVector2.x = theTransform.position.x;
                            companionVector2.y = theTransform.position.z;
                        
                            distanceFromTarget = (targetVector2 - companionVector2).sqrMagnitude;
                    
                          
                            if (target.layer == MyStaticGameObjects.monsterLayer && distanceFromTarget <= monsterAttackStopCompareDistance ||
                                target.layer != MyStaticGameObjects.monsterLayer && distanceFromTarget <= healingDistance)
                            {

                             
                                if (isHealer)
                                {
                                    myCharacterStatsCached = targetMyCharacterStats;
                                }
                                if (target.layer == MyStaticGameObjects.monsterLayer)
                                {
                                    yAxisDistance = targetTransform.position.y - theTransform.position.y;
                                    if (yAxisDistance < 0)
                                    {
                                        yAxisDistance = -yAxisDistance;
                                    }
                                    if (targetMyCharacterStats.GetHp() < 1 || !((MyMonsterNavMeshAgent)targetMyCharacterStats).isActive || yAxisDistance > 100)
                                    {
                                        target = null;
                                        targetTransform = null;
                                
                                        targetMyCharacterStats = null;
                                    }
                                    else
                                    {
                                        navMeshAgent.velocity = zeroVector;
                                        transform.LookAt(targetTransform.position);
                                        AttackAnimation();
                                        inAttackRange = true;
                                        navMeshAgent.isStopped = true;
                                    }
                                }
                                else if (isHealer 
                                    && (myCharacterStatsCached.GetHp() >= myCharacterStatsCached.GetMaxHP()
                                    && myCharacterStatsCached.hasRegen 
                                    && myCharacterStatsCached.hasAttackBuff 
                                    && myCharacterStatsCached.hasDefenseBuff
                                    ))
                                {
                                    //heal self
                                    if (hp < GetMaxHP() || !hasRegen || !hasDefenseBuff || !hasDefenseBuff)
                                    {
                                        target = gameObject;
                                        targetTransform = target.transform;
                                        navMeshAgent.velocity = zeroVector;
                                        AttackAnimation();
                                        inAttackRange = true;
                                        navMeshAgent.isStopped = true;
                                    }
                                    else
                                    {
                                        target = null;
                                        targetTransform = null;
                                        targetMyCharacterStats = null;
                                    

                                    }
                                }
                                else if (isHealer)
                                {
                                    //heal companions
                                    navMeshAgent.velocity = zeroVector;
                                    transform.LookAt(targetTransform.position);
                                    AttackAnimation();
                                    inAttackRange = true;
                                    navMeshAgent.isStopped = true;
                                }

                            }
                            else if (distanceFromTarget > maxDistance && target.layer == MyStaticGameObjects.monsterLayer)
                            {
                                target = null;
                                targetTransform = null;
                                targetMyCharacterStats = null;
                                Debug.Log("Monster is out of range.");
                              
                            }
                            else if(previousDestination!= targetTransform.position)
                            {
                                navMeshAgent.SetDestination(targetTransform.position);
                                previousDestination = targetTransform.position;
                               
                            }
                        }
                    }

                    if (null == target)
                    {
                        targetVector2.x = playerTransform.position.x;
                        targetVector2.y = playerTransform.position.z;

                        companionVector2.x = theTransform.position.x;
                        companionVector2.y = theTransform.position.z;
                      
                        if ((targetVector2 - companionVector2).sqrMagnitude > 4 || MyCharacterController.isEscaping)
                        {
                            navMeshAgent.SetDestination(playerTransform.position);
                            navMeshAgent.stoppingDistance = playerFollowStopDistance;
                            previousDestination = playerTransform.position;
                        }
                    }

                   
                }
               
            }

        }

        /// <summary>
        /// Stop the character movement if it is in attack, or in animation transition 
        /// Otherwise the character is in motion RunAnimation
        /// </summary>
        private void FixedUpdate()
        {

            if (!inAttackRange && hp>0)
            {
                changeInYAxis = previousYRotation - theTransform.rotation.eulerAngles.y;

                if (navMeshAgent.velocity.sqrMagnitude > .5f || changeInYAxis>3 || changeInYAxis < -3)
                {
                    RunAnimation();
                }
                else
                {
                    IdleAnimation();
                }
            }
            previousYRotation = theTransform.rotation.eulerAngles.y;
            if (navMeshAgent.enabled)
            {
                if (IsInAnimations(stationaryStates) || IsInTransition() || hp < 1)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.updateRotation = false;
                }
                else
                {
                    navMeshAgent.updateRotation = true;
                    navMeshAgent.isStopped = false;
                }
            }
        }

        /// <summary>
        /// Find Healing Target
        /// </summary>
        private void FindHealingTarget()
        {

            if (hp < GetMaxHP() || ((MyStaticGameObjects.instance.CheckIfAnyMonsterAreInRange() && (!hasRegen ||!hasAttackBuff||!hasDefenseBuff))))
            {
                SetTarget(gameObject);
            }
            else
            {
                for (int index = 0; index < MyStaticGameObjects.instance.companionsAndPlayer.Length; index++)
                {
                    healingGameObject = MyStaticGameObjects.instance.companionsAndPlayer[index];

                    targetVector2.x = healingGameObject.transform.position.x;
                    targetVector2.y = healingGameObject.transform.position.z;

                    companionVector2.x = theTransform.position.x;
                    companionVector2.y = theTransform.position.z;
                    //           if (healingGameObject != gameObject && Vector3.Distance(healingGameObject.transform.position, theTransform.position) < healingDistance)
                    if (healingGameObject != gameObject && (targetVector2 - companionVector2).sqrMagnitude < healingDistance)
                    {
                        myhealingCharacterStatsCached = healingGameObject.transform.gameObject.GetComponent<MyCharacterStats>();
                        if (!myhealingCharacterStatsCached.isInDeathTimeout())
                        {
                            if (myhealingCharacterStatsCached.GetHp() < myhealingCharacterStatsCached.GetMaxHP() ||
                            ((MyStaticGameObjects.instance.CheckIfAnyMonsterAreInRange() && (!myhealingCharacterStatsCached.hasRegen
                            || !myhealingCharacterStatsCached.hasAttackBuff
                            || !myhealingCharacterStatsCached.hasDefenseBuff))))
                            {
                                SetTarget(healingGameObject.transform.gameObject);
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the attack target of the companion
        /// </summary>
        /// <param name="theTarget"></param>
        protected internal override void SetTarget(GameObject theTarget)
        {
            if (!isInitialized)
            {
                Init();
            }
            if(target == null && hp > 0)
            {
                IdleAnimation();
            }
            if (!MyCharacterController.isEscaping)
            {
                if (target == theTarget)
                    return;
                if (theTarget != null && navMeshAgent.enabled)
                {
                    target = theTarget;
                    targetTransform = target.transform;
                    targetMyCharacterStats = target.GetComponent<MyCharacterStats>();
                    // navMeshAgent.ResetPath();
                    navMeshAgent.SetDestination(targetTransform.position);
                    navMeshAgent.stoppingDistance = monsterAttackStopDistance;
                }
            }
        }
       /// <summary>
       /// Set the IdleAnimation and stop other animations
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
            if (!isHealer)
            {
                animator.ResetTrigger("Attack1Toggle");
                animator.ResetTrigger("Attack2Toggle");
                animator.ResetTrigger("Attack3Toggle");
                animator.ResetTrigger("Attack4Toggle");
            }
            else
            {
                if (animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }
                if (animator.GetBool("Attack3"))
                {
                    animator.SetBool("Attack3", false);
                }
                if (animator.GetBool("Attack4"))
                {
                    animator.SetBool("Attack4", false);
                }
            }
            if (animator.GetBool("Die"))
            {
                animator.SetBool("Die", false);
            }
            idleState = true;
        }

        /// <summary>
        /// Start attack animations
        /// </summary>
        private void AttackAnimation()
        {
            
            if (!IsInAnimation(MyAnimationStates.attackState))
            {
                arrowType = (MyCompanionRangeWeapon.ArrowType)UnityEngine.Random.Range(0, 4);
                if (isHealer)
                {
                    if (!animator.GetBool("Attack1"))
                    {
                        animator.SetBool("Attack1", true);
                    }
                }
                else
                {
                    if ((int)arrowType == 0)
                    {
                        animator.SetTrigger("Attack1Toggle");
                    }
                    else if ((int)arrowType == 1)
                    {
                        animator.SetTrigger("Attack2Toggle");
                    }
                    else if ((int)arrowType == 2)
                    {
                        animator.SetTrigger("Attack3Toggle");
                    }
                    else if ((int)arrowType == 3)
                    {
                        animator.SetTrigger("Attack4Toggle");
                    }

                }
                if (animator.GetBool("Run"))
                {
                    animator.SetBool("Run", false);
                }

            }

        }

        /// <summary>
        /// Start Run Animation
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
            if (animator.GetBool("Attack1"))
            {
                animator.SetBool("Attack1", false);
            }
            if (!isHealer)
            {
                animator.ResetTrigger("Attack1Toggle");
                animator.ResetTrigger("Attack2Toggle");
                animator.ResetTrigger("Attack3Toggle");
                animator.ResetTrigger("Attack4Toggle");
            }
            else
            {
                if (animator.GetBool("Attack2"))
                {
                    animator.SetBool("Attack2", false);
                }
                if (animator.GetBool("Attack3"))
                {
                    animator.SetBool("Attack3", false);
                }
                if (animator.GetBool("Attack4"))
                {
                    animator.SetBool("Attack4", false);
                }
            }
        }

        /// <summary>
        ///  Start take Damage Animations
        /// </summary>
        private void TakeDamageAnimation()
        {

            animator.SetTrigger("TakeDamage");
        }

        /// <summary>
        /// Start the Death animations
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
            if (animator.GetBool("Run"))
            {
                animator.SetBool("Run", false);
            }

            if (!isHealer)
            {
                animator.ResetTrigger("Attack1Toggle");
                animator.ResetTrigger("Attack2Toggle");
                animator.ResetTrigger("Attack3Toggle");
                animator.ResetTrigger("Attack4Toggle");
               

                if (!isArcher)
                {
                    animator.ResetTrigger("Combo1");
                    animator.SetBool("InCombo", false);
                }
            }
            if (!isDead)
            {
                if (NavMesh.SamplePosition(theTransform.position + rayCastOffSet,out navMeshHit, 2, NavMesh.AllAreas))
                {
                    theTransform.position = navMeshHit.position;
                    theTransform.rotation = Quaternion.FromToRotation(Vector3.up, navMeshHit.normal);
                }
            
                isDead = true;
            }


        }
        /// <summary>
        /// Start Attack 2 Animation
        /// </summary>
        private void Attack2Animation()
        {

            if (!IsInAnimation(MyAnimationStates.attack2State))
            {
                if (animator.GetBool("Attack1"))
                {
                    animator.SetBool("Attack1", false);
                }
                if (!animator.GetBool("Attack2"))
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
        /// Handler for monster weapon hits the character
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision)
        {
            index = 0;
   
            if (collision.contacts[index].otherCollider.gameObject.layer == MyStaticGameObjects.monsterWeaponLayer)
            {
                if (!MyStaticGameObjects.instance.hasGameStarted)
                {

                    particle = MyObjectPools.onHitParticlePoolTransform.GetChild(MyObjectPools.instance.onHitParticlePool.GetMyObjectIndex()).gameObject;
                    particle.transform.position = collision.contacts[index].point + particleOffset;
                    particle.GetComponent<ParticleSystem>().Play();
                    onHitAudioSource.Play();
                    return;
                }

                myMonsterWeaponCached = collision.contacts[index].otherCollider.transform.GetComponent<MyMonsterWeapon>();
                myMonsterNavMeshAgentCached = myMonsterWeaponCached.attacker.GetComponent<MyMonsterNavMeshAgent>();

                if (myMonsterNavMeshAgentCached != null && myMonsterNavMeshAgentCached.IsWeaponAttacking())
                {
                    if (onHitAudioSource != null)
                    {
                        onHitAudioSource.Play();
                    }
                    if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Werewolf)
                    {
                        particle = MyObjectPools.wolfAttackParticlePoolTransform.GetChild(MyObjectPools.instance.wolfAttackParticlePool.GetMyObjectIndex()).gameObject;
                        particle.transform.position = collision.contacts[index].point + particleOffset;
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                    else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Bandit)
                    {
                        particle = MyObjectPools.banditAttackParticlePoolTransform.GetChild(MyObjectPools.instance.banditAttackParticlePool.GetMyObjectIndex()).gameObject;
                        particle.transform.position = collision.contacts[index].point + particleOffset;
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                    else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.Assassin)
                    {
                        particle = MyObjectPools.assassinAttackParticlePoolTransform.GetChild(MyObjectPools.instance.assassinAttackParticlePool.GetMyObjectIndex()).gameObject;
                        particle.transform.position = collision.contacts[index].point + particleOffset;
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                    else if (myMonsterNavMeshAgentCached.monsterType == MyMonsterNavMeshAgent.MonsterType.DarkKnight)
                    {
                        particle = MyObjectPools.darkknightAttackParticlePoolTransform.GetChild(MyObjectPools.instance.darkknightAttackParticlePool.GetMyObjectIndex()).gameObject;
                        particle.transform.position = collision.contacts[index].point + particleOffset;
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                    TakesDamage(myMonsterNavMeshAgentCached.GetAttackPoint());
                }
                if (!MyCharacterController.isEscaping)
                {
                    if (target == null || (target != null && (targetTransform.position- theTransform.position).sqrMagnitude > (targetTransform.position - collision.contacts[index].otherCollider.transform.position).sqrMagnitude))
                    {
                        SetTarget(myMonsterWeaponCached.attacker);
                    }
                }
            }
            else if (collision.contacts[index].otherCollider.gameObject.layer == MyStaticGameObjects.monsterWeaponRangeLayer)
            {
                monsterRangeWeaponCached = collision.contacts[index].otherCollider.transform.GetComponent<MyMonsterRangeWeapon>();
                if (monsterRangeWeaponCached != null)
                {
                    TakesDamage(monsterRangeWeaponCached.attackPoint);
                    if (!MyCharacterController.isEscaping)
                    {
                        if (target == null)
                        {
                            SetTarget(monsterRangeWeaponCached.attacker);
                        }
                    }
                }
            }   
        }
       
        /// <summary>
        /// Use by the preist to Select a character to Heal, Regen, Attack buff, and defense Buff. 
        /// </summary>
        public override void HealPlayers()
        {
            if (hp > 0 && isHealer && target != null && target.layer!= MyStaticGameObjects.monsterLayer)
            {
                myCharacterControllerHealingCached =targetMyCharacterStats;
                if (!myCharacterControllerHealingCached.isInDeathTimeout())
                {

                    if (myCharacterControllerHealingCached.GetHp() < myCharacterControllerHealingCached.GetMaxHP())
                    {
                        myCharacterControllerHealingCached.GiveHealth(GetAttackPoint());

                            particle = MyObjectPools.healParticlePoolTransform.GetChild(MyObjectPools.instance.healParticlePool.GetMyObjectIndex()).gameObject;
                            particle.transform.position = targetTransform.position + particleOffset;
                            particle.GetComponent<ParticleSystem>().Play(true);
                    }
                    else if (!myCharacterControllerHealingCached.hasRegen && MyStaticGameObjects.instance.CheckIfAnyMonsterAreInRange())
                    {
                        myCharacterControllerHealingCached.StartRegen(GetAttackPoint());
                    }
                    else if (!myCharacterControllerHealingCached.hasAttackBuff && MyStaticGameObjects.instance.CheckIfAnyMonsterAreInRange())
                    {
                        myCharacterControllerHealingCached.StartAttackBuff(GetAttackPoint());
                   
                    }
                    else if (!myCharacterControllerHealingCached.hasDefenseBuff && MyStaticGameObjects.instance.CheckIfAnyMonsterAreInRange())
                    {
                        myCharacterControllerHealingCached.StartDefenseBuff(GetAttackPoint());
                     
                    }
                 
                    if (weaponAudioSource != null && !weaponAudioSource.audioSource.isPlaying)
                        weaponAudioSource.Play();
                }
               
            }

        }

    }

}