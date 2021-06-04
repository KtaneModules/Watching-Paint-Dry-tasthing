using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class watchingPaintDry : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;
    public KMSelectable mainSelectable;

    public Renderer stroke;
    public Texture[] strokeTextures;

    private int color;
    private int strokeCount;
    private int rotationRange;
    private int solution;
    private int startTime;
    private int endTime;

    private bool submitted;
    private static readonly string[] colorNames = new string[] { "white", "red", "blue", "yellow", "black" };
    private static readonly string[] table1 = new string[] { "G", "E", "A", "C", "H", "G", "C", "F", "H", "B", "B", "E", "G", "D", "F", "D", "G", "H", "C", "A", "E", "C", "G", "A", "H", "D", "H", "D", "C", "F" };
    private static readonly int[] table2 = new int[] { 16, 25, 4, 32, 33, 29, 21, 6, 13, 24, 15, 11, 5, 10, 26, 17, 9, 12, 14, 23, 27, 7, 30, 31, 34, 3, 20, 28, 22, 8, 18, 19 };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        mainSelectable.OnFocus += delegate () { StartSelection(); };
        mainSelectable.OnDefocus += delegate () { EndSelection(); };
    }

    private void Start()
    {
        color = rnd.Range(0, 5);
        strokeCount = rnd.Range(3, 9);
        rotationRange = rnd.Range(0, 4);
        stroke.material.mainTexture = strokeTextures[color];
        Debug.LogFormat("[Watching Paint Dry #{0}] Number of strokes: {1}", moduleId, strokeCount);
        Debug.LogFormat("[Watching Paint Dry #{0}] Color of strokes: {1}", moduleId, colorNames[color]);
        Debug.LogFormat("[Watching Paint Dry #{0}] Stroke angle range: {1}° to {2}°", moduleId, 90 * rotationRange, 90 + 90 * rotationRange);
        var angle = 0f;
        switch (rotationRange)
        {
            case 0:
                angle = rnd.Range(10f, 85f);
                break;
            case 1:
                angle = rnd.Range(95f, 170f);
                break;
            case 2:
                angle = rnd.Range(190f, 265f);
                break;
            case 3:
                angle = rnd.Range(275f, 350f);
                break;
            default:
                throw new Exception("rotationRange has an invalid value (expected 0-3).");
        }
        stroke.transform.localEulerAngles = new Vector3(90f, angle, 0f);
        var letter = table1[(strokeCount - 3) * 5 + color];
        var letters = "ABCDEFGH";
        solution = table2[rotationRange * 8 + letters.IndexOf(letter)];
        Debug.LogFormat("[Watching Paint Dry #{0}] Number of seconds to watch the paint: {1}", moduleId, solution);
    }

    private void StartSelection()
    {
        if (moduleSolved)
            return;
        Debug.LogFormat("[Watching Paint Dry #{0}] Started watching at: {1}", moduleId, bomb.GetFormattedTime());
        startTime = (int)bomb.GetTime();
    }

    private void EndSelection()
    {
        if (submitted || moduleSolved)
            return;
        submitted = true; // For some reason, OnDefocus gets called twice, this bool accounts for that
        Debug.LogFormat("[Watching Paint Dry #{0}] Stopped watching at: {1}", moduleId, bomb.GetFormattedTime());
        endTime = (int)bomb.GetTime();
        var submittedTime = startTime - endTime;
        if (submittedTime < 0)
            submittedTime *= -1;
        Debug.LogFormat("[Watching Paint Dry #{0}] Elapsed time in seconds: {1}", moduleId, submittedTime);
        if (submittedTime == 0)
        {
            Debug.LogFormat("[Watching Paint Dry #{0}] Initiating paint strokes.", moduleId);
            StartCoroutine(PaintStrokes());
        }
        else if (submittedTime == solution)
        {
            module.HandlePass();
            moduleSolved = true;
            Debug.LogFormat("[Watching Paint Dry #{0}] You watched the paint dry for the correct amount of time. Module solved!", moduleId);
            StartCoroutine(WaitToToggle());
        }
        else
        {
            module.HandleStrike();
            Debug.LogFormat("[Watching Paint Dry #{0}] The paint is not happy with the amount of time for which it was watched. Strike!", moduleId);
            StartCoroutine(WaitToToggle());
        }
    }

    private IEnumerator PaintStrokes()
    {
        for (int i = 0; i < strokeCount; i++)
        {
            yield return new WaitForSeconds(rnd.Range(.75f, 1.75f));
            audio.PlaySoundAtTransform("stroke" + rnd.Range(1, 5), transform);
        }
        submitted = false;
    }

    private IEnumerator WaitToToggle()
    {
        yield return new WaitForSeconds(1.5f);
        submitted = false;
    }

    /*
    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} ";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        yield return null;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
    */
}
