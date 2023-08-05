using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class BarcodeCipherScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public TextMesh numberScreenText, inputScreenText;
    public TextMesh[] barcodeText;
    public KMSelectable UpArrow, DownArrow;
    public KMSelectable[] BarcodeButtons;

    private static int _moduleIdCounter = 1;
    private int _moduleId, inputScreenNumber = 0;
    private int[] numbers = new int[3], answerNumbers = new int[3];
    private bool _moduleSolved, textVisible = true;
    private bool[] barcodesSolved = new bool[3];
    private string screenNumber;
    private EdgeworkInfo[] edgework = new EdgeworkInfo[8];

    private struct EdgeworkInfo
    {
        public string Name;
        public int Value;

        public EdgeworkInfo(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        edgework[0] = new EdgeworkInfo("SERIAL NUMBER", BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).Sum());
        edgework[1] = new EdgeworkInfo("BATTERIES", BombInfo.GetBatteryCount());
        edgework[2] = new EdgeworkInfo("BATTERY HOLDERS", BombInfo.GetBatteryHolderCount());
        edgework[3] = new EdgeworkInfo("PORTS", BombInfo.GetPortCount());
        edgework[4] = new EdgeworkInfo("PORT PLATES", BombInfo.GetPortPlateCount());
        edgework[5] = new EdgeworkInfo("LIT INDICATORS", BombInfo.GetOnIndicators().Count());
        edgework[6] = new EdgeworkInfo("UNLIT INDICATORS", BombInfo.GetOffIndicators().Count());
        edgework[7] = new EdgeworkInfo("INDICATORS", BombInfo.GetIndicators().Count());

        UpArrow.OnInteract = UpArrowHandler;
        DownArrow.OnInteract = DownArrowHandler;

        for (int i = 0; i < BarcodeButtons.Length; i++)
        {
            int j = i;
            BarcodeButtons[i].OnInteract += delegate ()
            {
                BarcodeButtonHandler(j);
                return false;
            };
        }

        edgework.Shuffle();

        numbers[0] = Rnd.Range(0, 100);
        numbers[1] = Rnd.Range(0, 100);
        numbers[2] = Rnd.Range(0, 100);
        screenNumber = string.Format("{0:00}{1:00}{2:00}", numbers[0], numbers[1], numbers[2]);

        numberScreenText.text = screenNumber;

        var snLast = BombInfo.GetSerialNumber()[5] - '0';

        var numberSum = numbers[0].ToString().Sum(c => c - '0') + numbers[1].ToString().Sum(c => c - '0') + numbers[2].ToString().Sum(c => c - '0');

        for (int i = 0; i < 3; i++)
        {
            barcodeText[i].text = edgework[i].Name;
            answerNumbers[i] = (edgework[i].Value * (numberSum) ^ numbers[i]) % (snLast % 5 + 5);
        }

        Debug.LogFormat("[Barcode Cipher #{0}] The first barcode is {1} so the number required is {2}.", _moduleId, edgework[0].Name, edgework[0].Value);
        Debug.LogFormat("[Barcode Cipher #{0}] The second barcode is {1} so the number required is {2}.", _moduleId, edgework[1].Name, edgework[1].Value);
        Debug.LogFormat("[Barcode Cipher #{0}] The third barcode is {1} so the number required is {2}.", _moduleId, edgework[2].Name, edgework[2].Value);
        Debug.LogFormat("[Barcode Cipher #{0}] The sum of the displayed number's digits is {1}.", _moduleId, numberSum);
        Debug.LogFormat("[Barcode Cipher #{0}] The first barcode number × the sum of the 6-digit display = {1}.", _moduleId, edgework[0].Value * (numberSum));
        Debug.LogFormat("[Barcode Cipher #{0}] The second barcode number × the sum of the 6-digit display = {1}.", _moduleId, edgework[1].Value * (numberSum));
        Debug.LogFormat("[Barcode Cipher #{0}] The third barcode number × the sum of the 6-digit display = {1}.", _moduleId, edgework[2].Value * (numberSum));
        Debug.LogFormat("[Barcode Cipher #{0}] First result after XOR = {1}.", _moduleId, edgework[0].Value * (numberSum) ^ numbers[0]);
        Debug.LogFormat("[Barcode Cipher #{0}] Second result after XOR = {1}.", _moduleId, edgework[1].Value * (numberSum) ^ numbers[1]);
        Debug.LogFormat("[Barcode Cipher #{0}] Third result after XOR = {1}.", _moduleId, edgework[2].Value * (numberSum) ^ numbers[2]);
        Debug.LogFormat("[Barcode Cipher #{0}] The first answer is {1}.", _moduleId, answerNumbers[0]);
        Debug.LogFormat("[Barcode Cipher #{0}] The second answer is {1}.", _moduleId, answerNumbers[1]);
        Debug.LogFormat("[Barcode Cipher #{0}] The third answer is {1}.", _moduleId, answerNumbers[2]);
    }

    private void CheckSolved()
    {
        if (!_moduleSolved)
        {
            int s = 0;
            for (int i = 0; i < barcodesSolved.Length; i++)
            {
                if (barcodesSolved[i])
                    s++;
            }
            if (s == 3)
                StartCoroutine(Pass());
        }
    }

    private IEnumerator Pass()
    {
        Module.HandlePass();
        _moduleSolved = true;
        Audio.PlaySoundAtTransform("Solve", transform);
        barcodeText[0].text = "";
        yield return new WaitForSeconds(0.187f);
        barcodeText[1].text = "";
        yield return new WaitForSeconds(0.199f);
        barcodeText[2].text = "";
        yield return new WaitForSeconds(0.373f);
        inputScreenText.text = "";
        yield return new WaitForSeconds(0.187f);
        numberScreenText.text = "";
    }

    private void Strike()
    {
        Module.HandleStrike();
        textVisible = true;
        inputScreenText.text = inputScreenNumber.ToString();
        numberScreenText.text = screenNumber;
    }

    private bool BarcodeButtonHandler(int ix)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, BarcodeButtons[ix].transform);
        BarcodeButtons[ix].AddInteractionPunch(0.25f);
        if (!_moduleSolved)
        {
            if (!barcodesSolved[ix])
            {
                if (inputScreenNumber == answerNumbers[ix])
                {
                    barcodesSolved[ix] = true;
                    textVisible = false;
                    inputScreenText.text = "?";
                    numberScreenText.text = "??????";
                }
                else
                {
                    Debug.LogFormat("[Barcode Cipher #{0}] The answer was {1} but {2} was inputted. Strike!", _moduleId, answerNumbers[ix], inputScreenNumber);
                    Strike();
                }
            }
            CheckSolved();
        }
        return false;
    }

    private bool UpArrowHandler()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, UpArrow.transform);
        UpArrow.AddInteractionPunch(0.5f);
        if (!_moduleSolved)
        {
            if (inputScreenNumber < 9)
                inputScreenNumber++;
            if (textVisible)
                inputScreenText.text = inputScreenNumber.ToString();
        }
        return false;
    }

    private bool DownArrowHandler()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, DownArrow.transform);
        DownArrow.AddInteractionPunch(0.5f);
        if (!_moduleSolved)
        {
            if (inputScreenNumber > 0)
                inputScreenNumber--;
            if (textVisible)
                inputScreenText.text = inputScreenNumber.ToString();
        }
        return false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <answer1> <answer2> <answer3> [answers are: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.StartsWith("submit ")) command = command.Substring(7);
        else
        {
            yield return "sendtochaterror Submissions must start with the word submit.";
            yield break;
        }

        string[] list = command.Split(' ');
        for (int i = 0; i < list.Length; i++)
        {
            if (int.Parse(list[i]) > 9 || int.Parse(list[i]) < 0)
            {
                yield return "sendtochaterror Answers must be in the range of 0-9.";
                yield break;
            }

        }

        for (int i = 0; i < 3; i++)
        {
            while (int.Parse(list[i]) > inputScreenNumber)
            {
                UpArrow.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (int.Parse(list[i]) < inputScreenNumber)
            {
                DownArrow.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            BarcodeButtons[i].OnInteract();
            yield return new WaitForSeconds(0.25f);
        }

        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 3; i++)
        {
            if (barcodesSolved[i])
                continue;
            while (answerNumbers[i] > inputScreenNumber)
            {
                UpArrow.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (answerNumbers[i] < inputScreenNumber)
            {
                DownArrow.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            BarcodeButtons[i].OnInteract();
            yield return new WaitForSeconds(0.25f);
        }

        yield return null;
    }
}
