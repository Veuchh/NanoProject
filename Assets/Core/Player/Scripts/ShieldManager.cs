using Nano.Entity;
using Nano.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace Nano.Player
{
    public class ShieldManager : MonoBehaviour
    {
        [BoxGroup("COMPONENTS", ShowLabel = true)]
        [SerializeField] PlayerEntity player;
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

        [Button("ADD SHIELD")]
        public void AddShield(Data.BulletType _shieldType = Data.BulletType.Blue)
        {
            if (shieldList.Count >= maxNumberShield)
            {
                RemoveShield(shieldList[0]);
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
                    P1ShieldGet1_00_SFX.Post(gameObject);
                    break;
                case Data.BulletType.Blue:
                    _newShield.shieldRenderer.material.SetColor("_Color", Color.blue);
                    _newShield.shieldRenderer.material.SetTexture("_button_label_texture", inputSpriteList[1]);
                    P1ShieldGet2_00_SFX.Post(gameObject);
                    break;
                case Data.BulletType.Green:
                    _newShield.shieldRenderer.material.SetColor("_Color", Color.green);
                    _newShield.shieldRenderer.material.SetTexture("_button_label_texture", inputSpriteList[2]);
                    P1ShieldGet3_00_SFX.Post(gameObject);
                    break;
            }
            _newShield.shieldRenderer.material.SetFloat("_wiggle_seed", Random.Range(0.0f, 10.0f));
            _newShield.shieldRenderer.material.SetFloat("_bubble_angle", shieldList.Count);
        }

        public void RemoveShield(Shield _shield)
        {
            shieldList.Remove(_shield);
            player.playerData.shieldTypeList.Remove(player.playerData.shieldTypeList[0]);
            _shield.shieldRenderer.material.DOFloat(0.19f, "_wiggle_size", .1f);
            _shield.shieldRenderer.material.DOFloat(0f, "_circle_stroke_width", .1f);
            _shield.shieldRenderer.material.DOFloat(0f, "_bubble_size", .1f);
            _shield.transform.DOScale(_shield.fixedScale - 2f, .1f).OnComplete(() =>
            {
                _shield.shieldRenderer.material.DOColor(new Color(0,0,0,0), "_Color", .05f);
                _shield.transform.DOScale(_shield.fixedScale + 5f, .05f).OnComplete(() =>
                {
                    _shield.DOKill();
                    Destroy(_shield.gameObject);
                    shieldCollider.radius = .5f;
                });
            });
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
        }

        [Button("REMOVE ALL SHIELDS")]
        public void RemoveAllShields()
        {
            //if (shieldList.Count == 0) return;
            for (int i = 0; i < shieldList.Count; i++)
            {
                Shield _shield = shieldList[i];
                RemoveShield(_shield);
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
            if (_bullet != null)
            {
                if (shieldList.Count == 0) //NO SHIELD
                {
                    TakeDamage();
                    _bullet.ExplodeBullet();
                    //DAMAGE ?
                }
                else if (_bullet.bulletType == shieldList[0].shieldType) //GOOD SHIELD
                {
                    RemoveShield(shieldList[0]);

                    if (_bullet.convertingBullet)
                    {
                        _bullet.BackToSender();
                        //Add anim enemy teleport out
                        RecruitEnemy(_bullet);
                    }
                    else
                    {
                        //SHOW JUICY SCORE
                        _bullet.ExplodeBullet();
                        gameObject.GetComponent<PlayerScore>().IncreaseScoreHitNote();
                    }

                }
                else //WRONG SHIELD
                {
                    RemoveShield(shieldList[0]);
                    _bullet.ExplodeBullet();
                }
            }
        }

        private void RecruitEnemy(Bullet _bullet)
        {
            Animator animEnemy = _bullet.GetParentEnemy().GetComponentInChildren<Animator>();
            animEnemy.SetBool("enemyGotRecrutedOut", true);
            DOVirtual.DelayedCall(.5f, () => player.squadronManager.AddFollower());
        }

        private void TakeDamage()
        {
            spriteRenderer.material = damageMaterial;
            DOVirtual.DelayedCall(.2f, () => {
                spriteRenderer.material = basicMaterial;
                DOVirtual.DelayedCall(.1f, () =>
                {
                    spriteRenderer.material = damageMaterial;
                    DOVirtual.DelayedCall(.05f, () => {
                        spriteRenderer.material = basicMaterial;
                        DOVirtual.DelayedCall(.05f, () => {
                            spriteRenderer.material = damageMaterial;
                            DOVirtual.DelayedCall(.05f, () => {
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