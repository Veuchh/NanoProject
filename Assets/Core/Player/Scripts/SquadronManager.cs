using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Nano.Combat;

namespace Nano.Player
{
    public class SquadronManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float xOffset;
        [SerializeField] float yOffset;
        [SerializeField] float lerpStrength = .5f;
        [SerializeField] float followPlayerDelay = .05f;
        [SerializeField] float noiseSpeed = 1f;
        [SerializeField] float noiseStrength = 1f;
        [SerializeField] Transform followerObject;
        [SerializeField] PlayerEntity player;
        List<Transform> followers = new List<Transform>();
        public List<Vector3> storedPlayerPos = new List<Vector3>();
        float nextPosUpdate;

        [SerializeField] AK.Wwise.Event P1SummonFriend_00_SFX;
        [SerializeField] AK.Wwise.Event P2SummonFriend_00_SFX;
        [SerializeField] AK.Wwise.Event P1DamageBird_00_SFX;
        [SerializeField] AK.Wwise.Event P2DamageBird_00_SFX;


        private void Awake()
        {
            storedPlayerPos.Add(transform.position);
        }

        private void Update()
        {
            if (Time.time > nextPosUpdate)
            {
                List<Vector3> tempList = storedPlayerPos.ToList();
                for (int i = 1; i < storedPlayerPos.Count; i++)
                {
                    storedPlayerPos[i] = tempList[i - 1];
                }

                storedPlayerPos[0] = transform.position;

                nextPosUpdate = Time.time + followPlayerDelay;
            }

            for (int followerIndex = 0; followerIndex < followers.Count; followerIndex++)
            {
                Vector3 targetFollowerPosition = GetTargetPosition(followerIndex) + GetRandomNoise(followerIndex) * noiseStrength;

                followers[followerIndex].position = Vector3.Lerp(followers[followerIndex].position, targetFollowerPosition, lerpStrength * Time.deltaTime);
            }
        }

        private Vector3 GetTargetPosition(int followerIndex)
        {
            float xTargetPos = xOffset + xOffset * (followerIndex / 2);
            float yTargetPos = yOffset * (followerIndex / 2) * (followerIndex % 2 == 1 ? 1 : -1) + yOffset * (followerIndex % 2 == 1 ? 1 : -1);

            return storedPlayerPos[followerIndex / 2] + new Vector3(xTargetPos, yTargetPos, 0);
        }

        private Vector3 GetRandomNoise(int yPos)
        {
            float xNoise = Mathf.PerlinNoise(Time.time * noiseSpeed, yPos) - .5f;
            float yNoise = Mathf.PerlinNoise(Time.time * noiseSpeed, yPos * 5) - .5f;

            return new Vector3(xNoise, yNoise, 0);
        }

        [Button("ADD SQUADRON FOLLOWER")]
        public void AddFollower(bool combo3bullets = false)
        {
            (player.playerData.PlayerID == 1 ? P1SummonFriend_00_SFX : P2SummonFriend_00_SFX).Post(gameObject);
            Transform _newFollower = Instantiate(followerObject);
            followers.Add(_newFollower);
            gameObject.GetComponent<PlayerScore>().IncreaseScoreAddBird(combo3bullets);
            if (followers.Count % 2 == 1)
            {
                storedPlayerPos.Add(transform.position);
            }

            _newFollower.transform.position = GetTargetPosition(followers.IndexOf(_newFollower));
        }

        [Button("REMOVE SQUADRON FOLLOWER")]
        public void RemoveFollower()
        {
            (player.playerData.PlayerID == 1 ? P1DamageBird_00_SFX : P2DamageBird_00_SFX).Post(gameObject);

            Animator animHit = gameObject.transform.GetChild(4).GetComponent<Animator>();
            if (followers.Count == 0)
            {
                animHit.SetTrigger("playerIsHit");
                gameObject.GetComponent<PlayerScore>().DecreaseScoreRemoveBird();
                return;
            }
            Transform _follower = followers[followers.Count - 1];
            animHit.SetTrigger("playerIsHit");
            followers.Remove(_follower);
            gameObject.GetComponent<PlayerScore>().DecreaseScoreRemoveBird();
            _follower.DOScale(.9f, .2f).OnComplete(() =>
            {
                _follower.DOScale(1.1f, .1f).OnComplete(() =>
                {
                    _follower.DOKill();
                    Destroy(_follower.gameObject);
                });
            });
        }
    }
}