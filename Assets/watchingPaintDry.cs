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
    public KMColorblindMode Colorblind;

    public Renderer stroke;
    public Texture[] strokeTextures;
    public TextMesh cbText;

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
    private static Color[] cbColors = new Color[] { Color.white, Color.red, new Color(0, 0.075f, 0.5f), new Color(1, 0.85f, 0), Color.black };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private bool TwitchPlaysActive;
    private bool cbON;

    private Action start, end;
    private void Awake()
    {
        moduleId = moduleIdCounter++;
        start = delegate () { StartSelection(); };
        end = delegate () { EndSelection(); };
        mainSelectable.OnFocus += start;
        mainSelectable.OnDefocus += end;
        module.OnActivate += delegate ()
        {
            if (TwitchPlaysActive)
            {
                mainSelectable.OnFocus -= start;
                mainSelectable.OnDefocus -= end;
            }
            if (Colorblind.ColorblindModeActive)
                ToggleCB();
        };
    }

    private void Start()
    {
        color = rnd.Range(0, 5);
        strokeCount = rnd.Range(3, 9);
        rotationRange = rnd.Range(0, 4);
        stroke.material.mainTexture = strokeTextures[color];
        cbText.text = colorNames[color];
        cbText.color = cbColors[color];
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
                throw new ArgumentOutOfRangeException("rotationRange has an invalid value (expected 0-3).");
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
        var submittedTime = Math.Abs(startTime - endTime);
        Debug.LogFormat("[Watching Paint Dry #{0}] Elapsed time in seconds: {1}", moduleId, submittedTime);
        if (submittedTime == 0 || submittedTime == 1 || submittedTime == 2)
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
    private void ToggleCB()
    {
        cbON = !cbON;
        cbText.gameObject.SetActive(cbON);
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

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Use <!{0} inspect> to play the brush noises. Use <!{0} watch for 23> to watch the paint dry for 23 seconds. Use <!{0} colorblind> to toggle colorblind mode.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (command == "INSPECT")
        {
            yield return null;
            StartSelection();
            EndSelection();
        }
        else if (new string[] { "COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB" }.Contains(command))
        {
            yield return null;
            ToggleCB();
            yield break;
        }
        Match m = Regex.Match(command, @"^WATCH\s+(?:FOR\s+)?([1-3]?[0-9])$");
        if (m.Success)
        {
            yield return null;
            int startTime = (int)bomb.GetTime();
            int submit = int.Parse(m.Groups[1].Value);
            Debug.Log(submit);
            StartSelection();
            while (Math.Abs((int)bomb.GetTime() - startTime) != submit)
                yield return null;
            EndSelection();
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (submitted)
            yield return true;
        yield return ProcessTwitchCommand(string.Format("wait for {0}", solution));
    }
}
