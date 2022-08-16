using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace BuryTheLight
{
    public class MusicModule : LevelModule
    {
        public static MusicModule local;
        public float score;
        List<WaveSpawner> spawners = new List<WaveSpawner>();
        public float DRankScore;
        public float CRankScore;
        public float BRankScore;
        public float ARankScore;
        public float SRankScore;
        public float SSRankScore;
        public float SSSRankScore;
        public float MaxScore;
        public bool Announcer;
        public float OnHitMaxScoreGain;
        public float OnKillMaxScoreGain;
        public float OnParryMaxScoreGain;
        public float OnDeflectMaxScoreGain;
        public override IEnumerator OnLoadCoroutine()
        {
            local = this;
            EventManager.onCreatureHit += EventManager_onCreatureHit;
            EventManager.onCreatureKill += EventManager_onCreatureKill;
            EventManager.onCreatureParry += EventManager_onCreatureParry;
            EventManager.onDeflect += EventManager_onDeflect;
            return base.OnLoadCoroutine();
        }
        public override void Update()
        {
            base.Update();
            foreach (WaveSpawner spawner in WaveSpawner.instances)
            {
                if (spawner != null && !spawners.Contains(spawner))
                {
                    spawner.gameObject.AddComponent<MusicComponent>();
                    spawners.Add(spawner);
                }
            }
            if (score > 400) score = 400;
            score -= Time.deltaTime;
            if (score < 0) score = 0;
        }
        private void EventManager_onDeflect(Creature source, Item item, Creature target)
        {
            if(target == Player.local.creature && source != Player.local.creature)
            {
                score += Mathf.Clamp(item.rb.velocity.magnitude, 0, OnDeflectMaxScoreGain);
            }
        }

        private void EventManager_onCreatureParry(Creature creature, CollisionInstance collisionInstance)
        {
            if(creature != Player.local.creature)
            {
                score += Mathf.Clamp(collisionInstance.impactVelocity.magnitude, 0, OnParryMaxScoreGain);
            }
        }

        private void EventManager_onCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                if (creature != Player.local.creature)
                {
                    score += Mathf.Clamp(collisionInstance.damageStruct.damage, 0, OnKillMaxScoreGain);
                }
                if (creature == Player.local.creature)
                {
                    score = 0;
                }
            }
        }

        private void EventManager_onCreatureHit(Creature creature, CollisionInstance collisionInstance)
        {
            if(creature != Player.local.creature && !creature.isKilled)
            {
                score += Mathf.Clamp(collisionInstance.damageStruct.damage, 0, OnHitMaxScoreGain);
            }
            if(creature == Player.local.creature)
            {
                score *= 0.5f;
            }
        }
    }
    public class MusicComponent : MonoBehaviour
    {
        WaveSpawner spawner;
        string[] strings = {"Jenix.Opening", "Jenix.DRankTransition", "Jenix.DRank", "Jenix.DRankEnding", "Jenix.SRankTransition", "Jenix.SRank", "Jenix.SRankEnding", 
            "Jenix.AAnnouncer", "Jenix.BAnnouncer", "Jenix.CAnnouncer", "Jenix.DAnnouncer", "Jenix.SAnnouncer", "Jenix.SSAnnouncer", "Jenix.SSSAnnouncer"};
        Dictionary<string, AudioContainer> audioContainers = new Dictionary<string, AudioContainer>();
        List<AudioSource> audioSources = new List<AudioSource>();
        AudioSource announcer;
        double nextEventTime;
        int flip = 1;
        int index = 0;
        public float beatEnd = 6.4f;
        public float beatStart = 3.2f;
        bool running = false;
        bool hasDRankTransition = false;
        bool hasSRankTransition = false;
        bool hasD;
        bool hasC;
        bool hasB;
        bool hasA;
        bool hasS;
        bool hasSS;
        bool hasSSS;
        public void Start()
        {
            spawner = GetComponent<WaveSpawner>();
            spawner.OnWaveBeginEvent.AddListener(Replace);
            spawner.OnWaveAnyEndEvent.AddListener(Return);
            GameObject announcerObject = new GameObject();
            announcer = announcerObject.AddComponent<AudioSource>();
            announcer.outputAudioMixerGroup = GameManager.GetAudioMixerGroup(AudioMixerName.UI);
            announcer.loop = false;
            foreach (string address in strings)
            {
                Catalog.LoadAssetAsync<AudioContainer>(address, value =>
                {
                    if (!audioContainers.ContainsKey(address))
                        audioContainers.Add(address, value);
                }, "Bury The Light");
            }
            foreach (AudioSource source in spawner.gameObject.GetComponents<AudioSource>())
            {
                if (source != null && source.outputAudioMixerGroup != null && source.outputAudioMixerGroup == GameManager.GetAudioMixerGroup(AudioMixerName.Music))
                {
                    audioSources.Add(source);
                }
                else if (source != null && source.outputAudioMixerGroup != null && source.outputAudioMixerGroup == GameManager.GetAudioMixerGroup(AudioMixerName.UI)) announcer.volume = source.volume;
            }
            audioSources.Add(spawner.gameObject.AddComponent<AudioSource>());
            audioSources[1].volume = audioSources[0].volume;
            audioSources[0].loop = false;
            audioSources[1].outputAudioMixerGroup = GameManager.GetAudioMixerGroup(AudioMixerName.Music);
            nextEventTime = AudioSettings.dspTime - beatStart;
        }
        public void Update()
        {
            if (!running)
            {
                return;
            }
            else
            {
                double time = AudioSettings.dspTime;
                if (time + 1.6 > nextEventTime && MusicModule.local.score < MusicModule.local.DRankScore)
                {
                    if (hasDRankTransition)
                    {
                        index = 0;
                        hasSRankTransition = false;
                        hasDRankTransition = false;
                    }
                    index++;
                    audioSources[flip].clip = audioContainers["Jenix.Opening"].PickAudioClip(index);
                    audioSources[flip].PlayScheduled(nextEventTime - 1.6);
                    nextEventTime += audioContainers["Jenix.Opening"].PickAudioClip(index).length - 8;
                    flip = 1 - flip;
                    if (index + 1 >= audioContainers["Jenix.Opening"].sounds.Count) index = 0;
                    hasDRankTransition = false;
                }
                else if (MusicModule.local.score >= MusicModule.local.DRankScore && MusicModule.local.score < MusicModule.local.SRankScore && !hasDRankTransition)
                {
                    index = 0;
                    audioSources[flip].clip = audioContainers["Jenix.DRankTransition"].PickAudioClip(index);
                    nextEventTime = AudioSettings.dspTime;
                    audioSources[flip].PlayScheduled(nextEventTime);
                    nextEventTime += audioContainers["Jenix.DRankTransition"].PickAudioClip(index).length - 4.8;
                    flip = 1 - flip;
                    hasDRankTransition = true;
                }
                else if (time + 1.6 > nextEventTime && MusicModule.local.score < MusicModule.local.SRankScore && hasDRankTransition)
                {
                    if (hasSRankTransition)
                    {
                        index = 0;
                        hasSRankTransition = false;
                    }
                    audioSources[flip].clip = audioContainers["Jenix.DRank"].PickAudioClip(index);
                    audioSources[flip].PlayScheduled(nextEventTime - 1.6);
                    nextEventTime += audioContainers["Jenix.DRank"].PickAudioClip(index).length - 8;
                    flip = 1 - flip;
                    index++;
                    if (index >= audioContainers["Jenix.DRank"].sounds.Count) index = 0;
                    hasSRankTransition = false;
                }
                else if (MusicModule.local.score >= MusicModule.local.SRankScore && !hasSRankTransition)
                {
                    index = 0;
                    audioSources[flip].clip = audioContainers["Jenix.SRankTransition"].PickAudioClip(index);
                    nextEventTime = AudioSettings.dspTime;
                    audioSources[flip].PlayScheduled(nextEventTime);
                    nextEventTime += audioContainers["Jenix.SRankTransition"].PickAudioClip(index).length - 1.6;
                    flip = 1 - flip;
                    hasSRankTransition = true;
                }
                else if (time + 3.2 > nextEventTime && MusicModule.local.score >= MusicModule.local.SRankScore && hasSRankTransition)
                {
                    audioSources[flip].clip = audioContainers["Jenix.SRank"].PickAudioClip(index);
                    audioSources[flip].PlayScheduled(nextEventTime - 3.2);
                    nextEventTime += audioContainers["Jenix.SRank"].PickAudioClip(index).length - 9.6;
                    flip = 1 - flip;
                    index++;
                    if (index >= audioContainers["Jenix.SRank"].sounds.Count) index = 0;
                }
                if (MusicModule.local.Announcer)
                    if (MusicModule.local.score <= MusicModule.local.DRankScore)
                    {
                        hasD = false;
                        hasC = false;
                        hasB = false;
                        hasA = false;
                        hasS = false;
                        hasSS = false;
                        hasSSS = false;
                    }
                    else if (MusicModule.local.score >= MusicModule.local.DRankScore && MusicModule.local.score < MusicModule.local.CRankScore && (!hasD || hasC))
                    {
                        hasD = true;
                        hasC = false;
                        hasB = false;
                        hasA = false;
                        hasS = false;
                        hasSS = false;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.DAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.DAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.CRankScore && MusicModule.local.score < MusicModule.local.BRankScore && (!hasC || hasB))
                    {
                        hasD = true;
                        hasC = true;
                        hasB = false;
                        hasA = false;
                        hasS = false;
                        hasSS = false;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.CAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.CAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.BRankScore && MusicModule.local.score < MusicModule.local.ARankScore && (!hasB || hasA))
                    {
                        hasD = true;
                        hasC = true;
                        hasB = true;
                        hasA = false;
                        hasS = false;
                        hasSS = false;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.BAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.BAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.ARankScore && MusicModule.local.score < MusicModule.local.SRankScore && (!hasA || hasS))
                    {
                        hasD = true;
                        hasC = true;
                        hasB = true;
                        hasA = true;
                        hasS = false;
                        hasSS = false;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.AAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.AAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.SRankScore && MusicModule.local.score < MusicModule.local.SSRankScore && (!hasS || hasSS))
                    {
                        hasD = true;
                        hasC = true;
                        hasB = true;
                        hasA = true;
                        hasS = true;
                        hasSS = false;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.SAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.SAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.SSRankScore && MusicModule.local.score < MusicModule.local.SSSRankScore && (!hasSS || hasSSS))
                    {
                        hasD = true;
                        hasC = true;
                        hasB = true;
                        hasA = true;
                        hasS = true;
                        hasSS = true;
                        hasSSS = false;
                        announcer.clip = audioContainers["Jenix.SSAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.SSAnnouncer"].sounds);
                        announcer.Play();
                    }
                    else if (MusicModule.local.score >= MusicModule.local.SSSRankScore && !hasSSS)
                    {
                        hasD = true;
                        hasC = true;
                        hasB = true;
                        hasA = true;
                        hasS = true;
                        hasSS = true;
                        hasSSS = true;
                        announcer.clip = audioContainers["Jenix.SSSAnnouncer"].GetRandomAudioClip(audioContainers["Jenix.SSSAnnouncer"].sounds);
                        announcer.Play();
                    }
            }
        }
        public void Replace()
        {
            audioSources[1 - flip].clip = null;
            audioSources[1 - flip].Stop();
            audioSources[flip].clip = audioContainers["Jenix.Opening"].PickAudioClip(0);
            nextEventTime = AudioSettings.dspTime;
            audioSources[flip].PlayScheduled(nextEventTime);
            nextEventTime += audioContainers["Jenix.Opening"].PickAudioClip(0).length - 6.4;
            flip = 1 - flip;
            running = true;
            MusicModule.local.score = 0;
        }
        public void Return()
        {
            foreach (AudioSource source in spawner.gameObject.GetComponents<AudioSource>())
            {
                if (source != null && source.outputAudioMixerGroup != null && source.outputAudioMixerGroup == GameManager.GetAudioMixerGroup(AudioMixerName.UI))
                {
                    if (MusicModule.local.score < MusicModule.local.SRankScore)
                    {
                        source.clip = audioContainers["Jenix.DRankEnding"].PickAudioClip(0);
                    }
                    else if (MusicModule.local.score >= MusicModule.local.SRankScore)
                    {
                        source.clip = audioContainers["Jenix.SRankEnding"].PickAudioClip(0);
                    }
                    nextEventTime = AudioSettings.dspTime;
                    source.PlayScheduled(nextEventTime);
                }
            }
            Level.current.StartCoroutine(Utils.FadeOut(audioSources[1 - flip], 3f));
            flip = 1 - flip;
            index = 0;
            running = false;
            MusicModule.local.score = 0;
            hasDRankTransition = false;
            hasSRankTransition = false;
            hasD = false;
            hasC = false;
            hasB = false;
            hasA = false;
            hasS = false;
            hasSS = false;
            hasSSS = false;
        }
    }
}
