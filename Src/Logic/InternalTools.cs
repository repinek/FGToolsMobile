using FG.Common.CMS;
using FGClient.UI;
using NOTFGT.Localization;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using static FGClient.UI.UIModalMessage;

namespace NOTFGT.Logic
{
    public static class InternalTools
    {
        public static void DoModal(string title, string msg, ModalType type, OKButtonType btnType, Il2CppSystem.Action<bool> act = null, bool doSfx = true, string btnOkStr = null, TMPro.TextAlignmentOptions al = TMPro.TextAlignmentOptions.Center, float closeDelay = 0f)
        {
            if (btnOkStr != null)
                NewCmsStr("latest_btn_ok", btnOkStr);

            Il2CppSystem.IObservable<UniRx.Unit> acceptWaitObs = ModalMessageBaseData.CreateTimerObservable(closeDelay);
            string okStr = btnOkStr == null ? null : $"latest_btn_ok";

            var ModalMessageDataDisclaimer = new ModalMessageData
            {
                Title = title,
                Message = $"<size=70%>{msg}</size>",
                LocaliseTitle = LocaliseOption.NotLocalised,
                LocaliseMessage = LocaliseOption.NotLocalised,
                ModalType = type,
                OkButtonType = btnType,
                OnCloseButtonPressed = act,
                OkTextOverrideId = okStr,
                MessageTextAlignment = al,
                AcceptWaitObservable = acceptWaitObs,
                Priority = PopupMessagePriority.ExitGameOrReturnToTitleScreen,

            };

            PopupManager.Instance.Show(PopupInteractionType.Error, ModalMessageDataDisclaimer);
            if (doSfx)
                AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericPopUpAppears);
        }

        public static void NewCmsStr(string Key, string Value)
        {
            if (CMSLoader.Instance._localisedStrings.ContainsString(Key))
                CMSLoader.Instance._localisedStrings._localisedStrings.Remove(Key);
            CMSLoader.Instance._localisedStrings._localisedStrings.Add(Key, Value);
        }

        public static Sprite SetSpriteFromFile(string path, int Width, int Height)
        {
            byte[] ImageAsByte = File.ReadAllBytes(path);
            Texture2D Texture = new(Width, Height, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(Texture, ImageAsByte))
            {
                Texture.filterMode = FilterMode.Point;
                return Sprite.Create(Texture, new Rect(0.0f, 0.0f, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        public static string FormatException(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            return LocalizationManager.LocalizedString("generic_exception", [ex.Message, ex.StackTrace]);
        }

        public static string CleanStr(string strIN)
        {
            string strOUT = Regex.Replace(strIN, @"<.*?>|\t|\s{2,}", " ");
            strOUT = Regex.Replace(strOUT, @"(?<=<) | (?=>)", "");
            strOUT = strOUT.Trim();
            strOUT = Regex.Replace(strOUT, @"\s+", " ");
            return strOUT;
        }
    }
}
