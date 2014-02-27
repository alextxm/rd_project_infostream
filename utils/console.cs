using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace utils
{
    /// <summary>
    /// delegato per la gestione della pressione di un tasto scelta
    /// </summary>
    /// <typeparam name="T">tipologia dato</typeparam>
    /// <param name="arg">dato da passare alla funzione</param>
    public delegate void ChoiceFunctionDelegate<T>(T arg);

    /// <summary>
    /// risultato di una scelta di richiesta pressione tasto
    /// </summary>
    public enum ChoiceResult
    {
        /// <summary>
        /// tasto associato al comando OK
        /// </summary>
        Ok,

        /// <summary>
        /// tasto associato al comando CANCEL
        /// </summary>
        Cancel,

        /// <summary>
        /// tasto associato al comando ALTERNATIVE1
        /// </summary>
        Alternative1,

        /// <summary>
        /// tasto associato al comando ALTERNATIVE2
        /// </summary>
        Alternative2
    }

    /// <summary>
    /// utilità per l'interazione con la console
    /// </summary>
    public static class ConsoleUtils
    {
        /// <summary>
        /// scrive alle coordinate specificate della console
        /// </summary>
        /// <param name="s"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="origRow"></param>
        /// <param name="origCol"></param>
        /// <returns></returns>
        public static bool WriteAt(string s, int x, int y, int origRow, int origCol)
        {
            bool r = false;

            try
            {
                Console.SetCursorPosition(origCol + x, origRow + y);
                Console.Write(s);
                r = true;
            }
            catch (ArgumentOutOfRangeException e)
            {
            }

            return r;
        }

        /// <summary>
        /// mostra una richiesta all'utente e gli permette una risposta basata su 2 tasti
        /// </summary>
        /// <typeparam name="T">tipo dati</typeparam>
        /// <param name="msg">messaggio</param>
        /// <param name="displayKeys">true per visualizzare i tasti</param>
        /// <param name="arg">dato da passare agli hander</param>
        /// <param name="okKey">tasto per conferma</param>
        /// <param name="cancelKey">tasto per annullamento</param>
        /// <param name="okayDelegate">handler da chiamare in caso di conferma</param>
        /// <param name="cancelDelegate">handler da chiamare in caso di annullamento</param>
        public static void Choice<T>(string msg, bool displayKeys, T arg, ConsoleKey okKey, ConsoleKey cancelKey, ChoiceFunctionDelegate<T> okayDelegate, ChoiceFunctionDelegate<T> cancelDelegate)
        {
            ChoiceResult res = Choice(msg, displayKeys, okKey, cancelKey);

            if (res == ChoiceResult.Ok)
            {
                if(okayDelegate != null)
                    okayDelegate(arg);
            }
            else
            {
                if(cancelDelegate != null)
                    cancelDelegate(arg);
            }
        }

        /// <summary>
        /// mostra una richiesta all'utente e gli permette una risposta basata su 2 tasti
        /// </summary>
        /// <param name="msg">messaggio</param>
        /// <param name="displayKeys">true per visualizzare i tasti</param>
        /// <param name="okKey">tasto per conferma</param>
        /// <param name="cancelKey">tasto per annullamento</param>
        /// <returns>Ok in caso di conferma, Cancel in caso di annullamento</returns>
        public static ChoiceResult Choice(string msg, bool displayKeys, ConsoleKey okKey, ConsoleKey cancelKey)
        {
            Console.Write(msg);

            if (displayKeys)
            {
                Console.Write(" [{0}/{1}]", okKey, cancelKey);
            }

            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey(true);
            } while (cki.Key != okKey && cki.Key != cancelKey);

            Console.WriteLine(" [{0}]", cki.KeyChar);

            if (cki.Key == okKey)
                return ChoiceResult.Ok;
            else
                return ChoiceResult.Cancel;
        }

        /// <summary>
        /// mostra una richiesta all'utente e gli permette una risposta basata su 4 tasti
        /// </summary>
        /// <param name="msg">messaggio</param>
        /// <param name="displayKeys">true per visualizzare i tasti</param>
        /// <param name="okKey">tasto per conferma</param>
        /// <param name="cancelKey">tasto per annullamento</param>
        /// <param name="alternative1">tasto alternativo 1</param>
        /// <param name="alternative2">tasto alternativo 2</param>
        /// <returns>Ok in caso di conferma, Cancel in caso di annullamento, Alternative1, Alternative2</returns>
        public static ChoiceResult Choice(string msg, bool displayKeys, ConsoleKey okKey, ConsoleKey cancelKey, ConsoleKey alternative1, ConsoleKey alternative2)
        {
            Console.Write(msg);

            if (displayKeys)
            {
                Console.Write(" [{0}/{1}/{2}/{3}]", okKey, cancelKey, alternative1, alternative2);
            }

            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey(true);
            } while (cki.Key != okKey && cki.Key != cancelKey && cki.Key != alternative1 && cki.Key != alternative2);

            Console.WriteLine(" [{0}]", cki.KeyChar);

            if(cki.Key == okKey)
                return ChoiceResult.Ok;
            else if(cki.Key == cancelKey)
                return ChoiceResult.Cancel;
            else if(cki.Key == alternative1)
                return ChoiceResult.Alternative1;
            else
                return ChoiceResult.Alternative2;        
        }
    }
}
