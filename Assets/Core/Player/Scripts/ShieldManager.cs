using Nano.Entity;
using Nano.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;

namespace Nano.Player
{
    public class ShieldManager : MonoBehaviour
    {
        [BoxGroup("COMPONENTS", ShowLabel = true)]
        [SerializeField] PlayerEntity player;
        [SerializeField] PlayerScore playerScore;
        [BoxGroup("COMPONENTS", ShowLabel = true)]
        [SerializeField] SphereCollider shieldCollider;
        [BoxGroup("COMPONENTS", ShowLabel = true)]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Material damageMaterial;
        [SerializeField] Material basicMaterial;
        [SerializeField] Shield shieldPrefab;
        [SerializeField] float shieldSizeAugmentation;
        [SerializeField] float shieldResetTime;
        [SerializeField] float maxShieldSize;
        [SerializeField] float shieldCollisionAngleOffset;
        float shieldTimer;
        [SerializeField] int maxNumberShield;
        List<Shield> shieldList = new List<Shield>();
        [SerializeField] List<Texture> inputSpriteList = new List<Texture>();
        [SerializeField] AK.Wwise.Event P1ShieldGet1_00_SFX;
        [SerializeField] AK.Wwise.Event P1ShieldGet2_00_SFX;
        [SerializeField] AK.Wwise.Event P1ShieldGet3_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldGet1_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldGet2_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldGet3_00_SFX;
        [SerializeField] AK.Wwise.Event P1MainDamage_00_SFX;
        [SerializeField] AK.Wwise.Event P2MainDamage_00_SFX;
        [SerializeField] AK.Wwise.Event P1ShieldBreak1_00_SFX;
        [SerializeField] AK.Wwise.Event P1ShieldBreak2_00_SFX;
        [SerializeField] AK.Wwise.Event P1ShieldBreak3_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldBreak1_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldBreak2_00_SFX;
        [SerializeField] AK.Wwise.Event P2ShieldBreak3_00_SFX;
        [SerializeField] AK.Wwise.Event P1BulletShieldAbsorb_00_SFX;
        [SerializeField] AK.Wwise.Event P2BulletShieldAbsorb_00_SFX;
        [SerializeField] GameObject textFeedback;
        [SerializeField] GameObject collisionPrefab;

        int previousEnemyID = -1;
        int comboIndex = 0;

        [Button("ADD SHIELD")]
        public void AddShield(Data.BulletType _shieldType = Data.BulletType.Blue)
        {
            if (shieldList.Count >= maxNumberShield)
            {
                DissolveShield(shieldList[0]);
            }
            Shield _newShield = Instantiate(shieldPrefab, transform);
            _newShield.shieldType = _shieldType;
            _newShield.fixedScale = maxShieldSize - shieldSizeAugmentation * shieldList.Count;
            _newShield.transform.localScale = Vector3.zero;
            shieldList.Add(_newShield);
            player.playerData.shieldTypeList.Add(_newShield.shieldType);
            for (int i = 0; i < shieldList.Count; i++)
            {
                Shield _shield = shieldList[i];
                _shield.transform.DOScale(_shield.fixedScale + 0.3f, .3f).OnComplete(() =>
                {
                    _shield.transform.DOScale(_shield.fixedScale, .1f);
                });
            }
            if (shieldList.Count > 0) shieldCollider.radius = maxShieldSize / 2;
            shieldTimer = shieldResetTime;
            switch (_shieldType)
            {
                case Data.BulletType.Red:
                    _newShield.shieldRenderer.material.SetColor("_Color", Color.red);
                    _newShield.shieldRenderer.material.SetTexture("_button_label_texture", inputSpriteList[0]);
                    (player.playerData.PlayerID == 1 ? P1ShieldGet1_00_SFX : P2ShieldGet1_00_SFX).Post(gameObject);

                    break;
                case Data.BulletType.Blue:
                    _newShield.shieldRenderer.material.SetColor("_Color", Color.blue);
                    _newShield.shieldRenderer.material.SetTexture("_button_label_texture", inputSpriteList[1]);
                    (player.playerData.PlayerID == 1 ? P1ShieldGet2_00_SFX : P2ShieldGet2_00_SFX).Post(gameObject);
                    break;
                case Data.BulletType.Green:
                    _newShield.shieldRenderer.material.SetColor("_Color", Color.green);
                    _newShield.shieldRenderer.material.SetTexture("_button_label_texture", inputSpriteList[2]);
                    (player.playerData.PlayerID == 1 ? P1ShieldGet3_00_SFX : P2ShieldGet3_00_SFX).Post(gameObject);
                    break;
            }
            _newShield.shieldRenderer.material.SetFloat("_wiggle_seed", Random.Range(0.0f, 10.0f));
            _newShield.shieldRenderer.material.SetFloat("_bubble_angle", shieldList.Count);
        }

        public void DissolveShield(Shield _shield)
        {
            shieldList.Remove(_shield);
            player.playerData.shieldTypeList.Remove(player.playerData.shieldTypeList[0]);
            _shield.shieldRenderer.material.DOFloat(0.19f, "_wiggle_size", .1f);
            _shield.shieldRenderer.material.DOFloat(0f, "_circle_stroke_width", .1f);
            _shield.shieldRenderer.material.DOFloat(0f, "_bubble_size", .1f);
            _shield.transform.DOScale(_shield.fixedScale - 2f, .1f).OnComplete(() =>
            {
                _shield.shieldRenderer.material.DOColor(new Color(0, 0, 0, 0), "_Color", .05f);
                _shield.transform.DOScale(_shield.fixedScale + 5f, .05f).OnComplete(() =>
                {
                    _shield.DOKill();
                    Destroy(_shield.gameObject);
                });
            });
            UpdateAllShield();
        }

        public void BreakShield(Shield _shield)
        {
            shieldList.Remove(_shield);
            player.playerData.shieldTypeList.Remove(player.playerData.shieldTypeList[0]);
            _shield.shieldRenderer.material.DOFloat(0.8f, "_wiggle_size", .1f);
            _shield.shieldRenderer.material.DOFloat(0f, "_color_alpha", .1f);
            _shield.transform.DOScale(_shield.fixedScale + 5f, .1f).OnComplete(() =>
            {
                _shield.DOKill();
                Destroy(_shield.gameObject);
            });
            UpdateAllShield();
        }

        private void UpdateAllShield()
        {
            for (int i = 0; i < shieldList.Count; i++)
            {
                Shield _sizeFixShield = shieldList[i];
                _sizeFixShield.fixedScale = maxShieldSize - shieldSizeAugmentation * i;
                _sizeFixShield.transform.DOScale(_sizeFixShield.fixedScale + 0.3f, .3f).OnComplete(() =>
                {
                    _sizeFixShield.transform.DOScale(_sizeFixShield.fixedScale, .1f);
                });
                _sizeFixShield.shieldRenderer.material.DOFloat(i, "_bubble_angle", .2f);
            }
            if (shieldList.Count == 0) shieldCollider.radius = .5f;
        }

        [Button("REMOVE ALL SHIELDS")]
        public void RemoveAllShields()
        {
            //if (shieldList.Count == 0) return;
            for (int i = 0; i < shieldList.Count; i++)
            {
                Shield _shield = shieldList[i];
                DissolveShield(_shield);
            }
        }

        private void Update()
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0 && shieldList.Count > 0)
            {
                RemoveAllShields();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Bullet _bullet = other.GetComponent<Bullet>();
            //Here detect bullet type
            if (_bullet != null && !_bullet.hasCollided)
            {
                _bullet.hasCollided = true;
                if (shieldList.Count == 0) //NO SHIELD
                {
                    TakeDamage();
                    _bullet.ExplodeBullet();
                    DisplayFeedbackText("Ouch!");
                    previousEnemyID = -1;
                    comboIndex = 0;
                    (player.playerData.PlayerID == 1 ? P1MainDamage_00_SFX : P2MainDamage_00_SFX).Post(gameObject);
                }
                else if (_bullet.bulletType == shieldList[0].shieldType) //GOOD SHIELD
                {
                    DissolveShield(shieldList[0]);
                    (player.playerData.PlayerID == 1 ? P1BulletShieldAbsorb_00_SFX : P2BulletShieldAbsorb_00_SFX).Post(gameObject);

                    if (_bullet.ParentID != previousEnemyID)
                    {
                        comboIndex = 0;
                    }

                    previousEnemyID = _bullet.ParentID;
                    comboIndex++;

                    string feedbackToDisplay = "";

                    //TODO DisplayText
                    switch (comboIndex)
                    {
                        case 1:
                            feedbackToDisplay = "Good";
                            break;
                        case 2:
                            feedbackToDisplay = "Great";
                            break;
                        case 3:
                            feedbackToDisplay = "Awesome !";
                            break;
                        case > 3:
                            feedbackToDisplay = "Divine !";
                            break;
                        default:
                            break;
                    }

                    DisplayFeedbackText(feedbackToDisplay);

                    playerScore.IncreaseScoreHitNote(comboIndex);

                    if (_bullet.convertingBullet)
                    {
                        string textToDisplay = "Rallied !";
                        _bullet.BackToSender(textToDisplay);
                        RecruitEnemy(_bullet, true);
                    }
                    else
                    {
                        _bullet.ExplodeBullet();
                    }

                }
                else //WRONG SHIELD
                {
                    if (player.InputHandler.gamepad != null)
                        player.InputHandler.gamepad.SetMotorSpeeds(0, 10);

                    DOVirtual.DelayedCall(.15f, () =>
                    {
                        if (player.InputHandler.gamepad != null) player.InputHandler.gamepad.SetMotorSpeeds(0, 0);
                    });

                    switch (shieldList[0].shieldType)
                    {
                        case Data.BulletType.Red:
                            (player.playerData.PlayerID == 1 ? P1ShieldBreak1_00_SFX : P2ShieldBreak1_00_SFX).Post(gameObject);
                            break;
                        case Data.BulletType.Blue:
                            (player.playerData.PlayerID == 1 ? P1ShieldBreak2_00_SFX : P2ShieldBreak2_00_SFX).Post(gameObject);
                            break;
                        case Data.BulletType.Green:
                            (player.playerData.PlayerID == 1 ? P1ShieldBreak3_00_SFX : P2ShieldBreak3_00_SFX).Post(gameObject);
                            break;
                    }

                    BreakShield(shieldList[0]);
                    _bullet.ExplodeBullet();
                    DisplayFeedbackText("Ouch!");
                    previousEnemyID = -1;
                    comboIndex = 0;
                    (player.playerData.PlayerID == 1 ? P1MainDamage_00_SFX : P2MainDamage_00_SFX).Post(gameObject);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(collision.gameObject.name);
            ShieldManager _shield = collision.gameObject.GetComponent<ShieldManager>();
            if (_shield != null && shieldList.Count > 0 && _shield.shieldList.Count > 0)
            {
                ContactPoint _contact = collision.contacts[0];
                GameObject _object = Instantiate(collisionPrefab, _contact.point, Quaternion.identity);
                //_object.GetComponentInChildren<SpriteRenderer>().color = shieldList[0].shieldRenderer.material.color;
                float angle = -(Vector2.SignedAngle(_shield.transform.position - transform.position, Vector2.up) * (Mathf.PI * 2)) / 360 ;
                shieldList[0].shieldRenderer.material.SetFloat("_anglewiggle_angle", angle - shieldCollisionAngleOffset);
                if (shieldList.Count > 0)
                {
                    shieldList[0].shieldRenderer.material.DOFloat(1, "_anglewiggle_size", .5f).OnComplete(() =>
                    {
                        shieldList[0].shieldRenderer.material.DOFloat(0, "_anglewiggle_size", .2f);
                    });
                }
            }
        }


        void DisplayFeedbackText(string textToDisplay)
        {
            Instantiate(textFeedback, transform.position, Quaternion.Euler(0,0,Random.Range(-20f,20f)), transform).GetComponentInChildren<TextMeshPro>().text = textToDisplay;
        }

        private void RecruitEnemy(Bullet _bullet, bool combo3bullets = false)
        {
            Animator animEnemy = _bullet.GetParentEnemy().GetComponentInChildren<Animator>();
            animEnemy.SetBool("enemyGotRecrutedOut", true);
            DOVirtual.DelayedCall(.5f, () => player.squadronManager.AddFollower(combo3bullets));
        }

        private void TakeDamage()
        {
            spriteRenderer.material = damageMaterial;
            if (player.InputHandler.gamepad != null) player.InputHandler.gamepad.SetMotorSpeeds(10, 10);
            DOVirtual.DelayedCall(.2f, () =>
            {
                spriteRenderer.material = basicMaterial;
                DOVirtual.DelayedCall(.1f, () =>
                {
                    if (player.InputHandler.gamepad != null) player.InputHandler.gamepad.SetMotorSpeeds(0, 0);
                    spriteRenderer.material = damageMaterial;
                    DOVirtual.DelayedCall(.05f, () =>
                    {
                        spriteRenderer.material = basicMaterial;
                        DOVirtual.DelayedCall(.05f, () =>
                        {
                            spriteRenderer.material = damageMaterial;
                            DOVirtual.DelayedCall(.05f, () =>
                            {
                                spriteRenderer.material = basicMaterial;

                            });
                        });
                    });
                });
            });
            transform.DOScale(1.3f, .1f).OnComplete(() =>
            {
                transform.DOScale(1, .1f);
            });
            player.squadronManager.RemoveFollower();
        }
    }
}