using System.Collections;
using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Loop: Repeatedly tells IBehaviorOrchestration to execute actions.
/// 
/// BehaviorLoop's ONLY responsibility is looping and timing.
/// It does NOT manage actions or execution logic.
/// 
/// Usage:
/// 1. Attach BehaviorLoop to actor GameObject
/// 2. Attach an IBehaviorOrchestration implementation (manages and executes actions)
/// 3. Attach concrete actions (they auto-subscribe to the orchestrator)
/// 4. BehaviorLoop tells the orchestrator when to execute
/// </summary>
public class BehaviorLoop : MonoBehaviour
{
    public enum LoopMode
    {
        Once = 0,
        Loop = 1,
        LoopNTimes = 2
    }

    public LoopMode Mode = LoopMode.Loop;

    public bool StartOnStart = true;

    [SerializeField]
    private int _loopCount = 1;

    [SerializeField]
    private float _delayBetweenLoopsSeconds = 0f;


    private IBehaviorOrchestration _orchestration;
    private Coroutine _loopCoroutine;
    private bool _isRunning = false;

    private void Start()
    {
        _orchestration = GetComponent<IBehaviorOrchestration>();
        
        if (_orchestration == null)
        {
            BehaviorSystemLogger.LogError(
                $"BehaviorLoop on {gameObject.name} could not find IBehaviorOrchestration component.",
                gameObject);
            return;
        }

        if (StartOnStart)
        {
            StartBehaviorLoop();
        }
    }

    private void OnDisable()
    {
        StopBehaviorLoop();
    }

    public void StartBehaviorLoop()
    {
        if (_isRunning)
        {
            BehaviorSystemLogger.LogWarning($"BehaviorLoop on {gameObject.name} is already running.", gameObject);
            return;
        }

        if (_orchestration == null)
        {
            BehaviorSystemLogger.LogError($"BehaviorLoop on {gameObject.name} has no orchestration strategy.", gameObject);
            return;
        }

        _isRunning = true;
        _loopCoroutine = StartCoroutine(ExecuteBehaviorLoop());
    }

    public void StopBehaviorLoop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
    }

    private IEnumerator ExecuteBehaviorLoop()
    {
        int iterationCount = 0;

        while (_isRunning)
        {
            if (Mode == LoopMode.Once && iterationCount > 0)
                break;

            if (Mode == LoopMode.LoopNTimes && iterationCount >= _loopCount)
                break;

            // Tell the orchestrator to execute - that's it, that's our only job
            yield return _orchestration.ExecuteActions(gameObject);

            iterationCount++;

            if (_delayBetweenLoopsSeconds > 0 && _isRunning)
            {
                yield return new WaitForSeconds(_delayBetweenLoopsSeconds);
            }
        }

        _isRunning = false;
    }
}



    



