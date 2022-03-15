// Decompiled with JetBrains decompiler
// Type: TwoRatChat.Voice.RatSpeech
// Assembly: TwoRatChat.Voice, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C59EE4B4-BBB1-4FC6-8582-A3D6C2115370
// Assembly location: X:\VSNEW\badmonster\TwoRatChat\TwoRatChat.Main\bin\x86\Debug\TwoRatChat.Voice.dll

using CSCore;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using TwoRatChat.Interfaces;

namespace TwoRatChat.Voice
{
  public class RatSpeech : IVoiceEngine
  {
    private SpeechRecognitionEngine engine;
    private SpeechSynthesizer _synth = new SpeechSynthesizer();
    private MemoryStream synthMemoryStream = new MemoryStream();
    private Dictionary<object, Grammar> _cmdGrammars = new Dictionary<object, Grammar>();
    private Dictionary<string, object> _cmdAliases = new Dictionary<string, object>();
    private CultureInfo culture;
    private WaveOut waveOut;
    private MediaFoundationDecoder waveSource;

    public void BeginInitialize(string locale)
    {
      this.culture = new CultureInfo(locale);
      this.engine = new SpeechRecognitionEngine(this.culture);
      this.engine.SetInputToDefaultAudioDevice();
      this.engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(this.engine_SpeechRecognized);
      this.engine.UpdateRecognizerSetting("AdaptationOn", 1);
      this.engine.UpdateRecognizerSetting("PersistedBackgroundAdaptation", 1);
      this.engine.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", 70);
      this.Register((object) null, new CultureInfo("ru-RU"), "X019", "89");
      this._synth.SetOutputToWaveStream((Stream) this.synthMemoryStream);
    }

    public void EndInitialize()
    {
      if (this._cmdAliases.Count <= 0)
        return;
      this.engine.RecognizeAsync(RecognizeMode.Multiple);
    }

    public void SetupDevice(int deviceId)
    {
      this.waveSource = new MediaFoundationDecoder((Stream) this.synthMemoryStream);
      this.waveOut = new WaveOut()
      {
        Device = new WaveOutDevice(deviceId)
      };
      this.waveOut.Initialize((IWaveSource) this.waveSource);
    }

    public void Talk(string _voice = "autoselect", string _text = "")
    {
      if (_voice == "autoselect")
        this._synth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult, 0, this.culture);
      else
        this._synth.SelectVoice(_voice);
      this._synth.SpeakAsync(_text);
      this.waveOut.Play();
    }

    private void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
      object obj;
      if (e.Result == null || (double) e.Result.Confidence <= 0.7 || !this._cmdAliases.TryGetValue(e.Result.Text, out obj) || this.OnRecognize == null || obj == null)
        return;
      this.OnRecognize(obj);
    }

    public void Register(
      object actuator,
      CultureInfo locale,
      string start,
      params string[] choices)
    {
      GrammarBuilder builder = new GrammarBuilder(start);
      builder.Culture = locale;
      Choices alternateChoices = new Choices();
      alternateChoices.Add(choices);
      builder.Append(alternateChoices);
      Grammar grammar = new Grammar(builder);
      grammar.Name = "main";
      this.engine.LoadGrammar(grammar);
      for (int index = 0; index < choices.Length; ++index)
        this._cmdAliases[start + " " + choices[index]] = actuator;
      if (actuator == null)
        return;
      this._cmdGrammars[actuator] = grammar;
    }

    public void Unregister(object actuator)
    {
      Grammar grammar;
      if (actuator == null || !this._cmdGrammars.TryGetValue(actuator, out grammar))
        return;
      if (!grammar.Loaded)
        return;
      try
      {
        this.engine.UnloadGrammar(grammar);
      }
      catch
      {
      }
    }

    public List<string> Voices
    {
      get
      {
        List<string> voices = new List<string>();
        foreach (InstalledVoice installedVoice in this._synth.GetInstalledVoices())
          voices.Add(installedVoice.VoiceInfo.Name);
        return voices;
      }
    }

    public event Action<object> OnRecognize;
  }
}
