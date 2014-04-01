using System;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace utils
{
    /// <summary>
    /// modalità di assemblaggio delle variabili della linea di comando
    /// </summary>
    public enum CommandLineArgsAssembleOrder
    {
        /// <summary>
        /// prima le opzioni quindi gli argomenti fissi
        /// </summary>
        OptionsThenArguments,

        /// <summary>
        /// prima gli argomenti fissi quindi le opzioni
        /// </summary>
        ArgumentsThenOptions
    }

    /// <summary>
    /// classe che gestisce le opzioni (viene istanziata solo da CommandLineArgs)
    /// </summary>
    public sealed class CommandLineArgsOptions
    {
        private string optionsPrefix = "-";
        private char optionsCompositeSeparator = '=';

        private Dictionary<string, string> options = new Dictionary<string, string>();

        #region proprietà
        /// <summary>
        /// numero di opzioni disponibili
        /// </summary>
        public int Count
        {
            get { return options.Count; }
        }

        /// <summary>
        /// accesso ad una singola opzione
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string this[string index]
        {
            get
            {
                return GetValue(index);
            }
        }
        #endregion

        #region costruttori
        internal CommandLineArgsOptions(string optionsPrefix, char optionsCompositeSeparator, Dictionary<string, string> rawoptions)
        {
            this.optionsPrefix = optionsPrefix;
            this.optionsCompositeSeparator = optionsCompositeSeparator;
            this.options = rawoptions;
        }
        #endregion

        #region metodi pubblici
        /// <summary>
        /// Controlla se una determinata opzione e' stata specificata (e quindi "attivata") dall'utente
        /// </summary>
        /// <param name="option">opzione da controllare</param>
        /// <returns></returns>
        public bool IsOptionEnabled(string option)
        {
            if (String.IsNullOrEmpty(option))
                return false;

            string opt = (String.Compare(option.Substring(0, 1), optionsPrefix, StringComparison.Ordinal) != 0) ? option : option.Substring(0, 1);
            return options.ContainsKey(opt);
        }

        /// <summary>
        /// Controlla se almeno una delle opzioni specificate e' attiva
        /// </summary>
        /// <param name="optionsToCheck">opzioni da controllare</param>
        /// <returns></returns>
        public bool IsAnyOptionEnabled(IEnumerable<string> optionsToCheck)
        {
            if (optionsToCheck == null)
                return false;

            foreach (string option in optionsToCheck)
            {
                string opt = (String.Compare(option.Substring(0, 1), optionsPrefix, StringComparison.Ordinal) != 0) ? option : option.Substring(0, 1);

                if (this.options.ContainsKey(opt) == true)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Controlla se tutte le opzioni specificate sono attive o meno
        /// </summary>
        /// <param name="optionsToCheck"></param>
        /// <returns></returns>
        public bool AreAllOptionsEnabled(IEnumerable<string> optionsToCheck)
        {
            if (optionsToCheck == null)
                return false;

            foreach (string option in optionsToCheck)
            {
                string opt = (String.Compare(option.Substring(0, 1), optionsPrefix, StringComparison.Ordinal) != 0) ? option : option.Substring(0, 1);

                if (this.options.ContainsKey(opt) == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// restituisce il valore di una opzione attivata
        /// se l'opzione non e' attiva, viene restituito null
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetValue(string option)
        {
            if (IsOptionEnabled(option) == false)
                return null;

            return this.options[option];
        }

        /// <summary>
        /// converte in Dictionary<string,string>
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> ToDictionary()
        {
            return options;
        }
        #endregion
    }

    /// <summary>
    /// classe che effettua la gestione degli argomenti forniti a linea di comando
    /// </summary>
    public sealed class CommandLineArgs
	{
        private string optionsPrefix = "-";
        private char optionsCompositeSeparator = '=';

        //private Dictionary<string, string> options = new Dictionary<string, string>();
        private CommandLineArgsOptions options = null;
        private Dictionary<int, string> arguments = new Dictionary<int, string>();

        private string[] unparsedArguments = null;

        #region proprieta'
        /// <summary>
        /// opzioni
        /// </summary>
        public CommandLineArgsOptions Options
        {
            get
            {
                return options;
            }
        }

        /// <summary>
        /// argomenti
        /// </summary>
        public Collection<string> Arguments
        {
            get
            {
                Collection<string> res = new Collection<string>();
                foreach (string s in arguments.Values)
                    res.Add(s);

                return res;
            }
        }
        #endregion

        #region costruttori
        /// <summary>
        /// Costruttore
        /// </summary>
        /// <param name="args"></param>
		public CommandLineArgs(string []args)
        {
            unparsedArguments = args;

            Parse();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="optPrefix"></param>
        /// <param name="optCompositeSep"></param>
        public CommandLineArgs(string[] args, string optPrefix, char optCompositeSep)
        {
            unparsedArguments = args;

            if (optPrefix!=null && optPrefix.Length > 0 && 
                String.Compare(optionsPrefix, optPrefix, StringComparison.Ordinal) != 0)
                optionsPrefix = optPrefix;

            if (optionsCompositeSeparator.CompareTo(optCompositeSep) != 0)
                optionsCompositeSeparator = optCompositeSep;

            Parse();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="collisionsFixingRules"></param>
        public CommandLineArgs(string[] args, string[] collisionsFixingRules)
        {
            unparsedArguments = args;

            Parse();
            FixListCollisions(collisionsFixingRules, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="optPrefix"></param>
        /// <param name="optCompositeSep"></param>
        /// <param name="collisionsFixingRules"></param>
        public CommandLineArgs(string[] args, string optPrefix, char optCompositeSep, string[] collisionsFixingRules)
        {
            unparsedArguments = args;

            if (optPrefix != null && optPrefix.Length > 0 &&
                String.Compare(optionsPrefix, optPrefix, StringComparison.Ordinal) != 0)
                optionsPrefix = optPrefix;

            if (optionsCompositeSeparator.CompareTo(optCompositeSep) != 0)
                optionsCompositeSeparator = optCompositeSep;

            Parse();
            FixListCollisions(collisionsFixingRules, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optPrefix"></param>
        /// <param name="optCompositeSep"></param>
        public CommandLineArgs(string optPrefix, char optCompositeSep)
        {
            if (optPrefix != null && optPrefix.Length > 0 &&
                String.Compare(optionsPrefix, optPrefix, StringComparison.Ordinal) != 0)
                optionsPrefix = optPrefix;

            if (optionsCompositeSeparator.CompareTo(optCompositeSep) != 0)
                optionsCompositeSeparator = optCompositeSep;
        }
        #endregion

        /// <summary>
        /// Effettua il parsing degli argomenti suddividendoli in opzioni e argomenti a
        /// seconda del prefix e separator specificati
        /// </summary>
        private void Parse()
        {
            Dictionary<string, string> rawoptions = new Dictionary<string, string>();

            if (unparsedArguments == null || unparsedArguments.Length < 1)
            {
                options = new CommandLineArgsOptions(optionsPrefix, optionsCompositeSeparator, rawoptions);
                return;
            }

            int i = -1;
            foreach (string arg in unparsedArguments)
            {
				if(String.IsNullOrEmpty(arg.Trim()))
					continue;

                if(String.Compare(arg.Substring(0, optionsPrefix.Length), optionsPrefix, StringComparison.Ordinal) != 0)
                {
                    // this is not an option, add it to the args list
                    arguments.Add(++i, arg);
                    continue;
                }

                string opt = arg.Substring(optionsPrefix.Length); //without prefix
                if (opt.IndexOf(optionsCompositeSeparator) >= 1)
                {
                    // option is composite
                    string[] sep = opt.Split(new char[] { optionsCompositeSeparator });
                    if (sep.Length != 2)
                        continue; //invalid, skip it

                    // aggiorna se esiste oppure aggiungi
                    if (rawoptions.ContainsKey(sep[0]) == true)
                        rawoptions[sep[0]] = sep[1];
                    else
                        rawoptions.Add(sep[0], sep[1]);

                    continue;
                }

                // l'opzione NON e' composita
                if (rawoptions.ContainsKey(opt) == false)
                    rawoptions.Add(opt, System.String.Empty);
            }

            // pulisci alla fine
            unparsedArguments = null;

            options = new CommandLineArgsOptions(optionsPrefix, optionsCompositeSeparator, rawoptions);
        }

        /// <summary>
        /// Risolve le collisioni fra opzioni
        ///
        /// le regole devono avere il seguente formato:
        ///
        /// list-of-colliders|winner
        ///         oppure
        /// list-of-colliders!winner
        ///
        /// dove list-of-colliders e' una lista di opzioni SENZA prefisso separata da ,
        /// ! indica che winner verra' identificato sulla base di un matching parziale
        /// | indica che winner verra' identificato sulla base di un matching letterale
        ///
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="savePrivate"></param>
        private Dictionary<string, string> FixListCollisions(string[] rules, bool savePrivate)
        {
            Dictionary<string, string> outlist = new Dictionary<string, string>();

            if (rules.Length < 1)
                return null;

            // crea una copia della lista originale su cui lavorare
            outlist = options.ToDictionary();

            foreach (string rule in rules)
            {
                int sep = rule.IndexOfAny(new char[] { '|', '!' });
                if (sep < 2)
                    continue;

                bool fullMatch = rule[sep].CompareTo('|') == 0 ? true : false;
                string[] parts = rule.Split(new char[] { rule[sep] });
                int checkCount = 0;

                List<string> metaColliders = new List<string>();
                foreach (string m in parts[0].Split(new char[] { ',' }))
                {
                    // valore per il controllo presenza metacolliders (vedi sotto)
                    if (outlist.ContainsKey(m) == true)
                        checkCount++;

                    metaColliders.Add(m);
                }

                // controlla che tutti i metacolliders sia contenuto nella
                // list, altrimenti possiamo gia' passare alla prossima regola
                if (checkCount < metaColliders.Count)
                    continue;

                if (fullMatch == true)
                {
                    // errore: winner deve essere per forza tra i metacolliders
                    if (metaColliders.Contains(parts[1]) == false)
                        continue;

                    // aggiungi il vincitore se necessario
                    if (outlist.ContainsKey(parts[1]) == false)
                        outlist.Add(parts[1], System.String.Empty);

                    // rimuovi i perdenti
                    foreach (string m in metaColliders)
                    {
                        if(String.Compare(m, parts[1], StringComparison.Ordinal) != 0 && outlist.ContainsKey(m) == true)
                            outlist.Remove(m);
                    }
                }
                else
                {
                    // compariamo ogni collider con winner per match da 0 a lunghezza di winner
                    // si ferma al primo matching
                    foreach (string m in metaColliders)
                    {
                        if (m.Length < parts[1].Length)
                            continue;

                        if(String.Compare(m.Substring(0, parts[1].Length), parts[1], StringComparison.Ordinal) == 0)
                        {
                            // aggiungi il vincitore se necessario
                            if (outlist.ContainsKey(m) == false)
                                outlist.Add(m, System.String.Empty);

                            // rimuovi i perdenti
                            foreach (string s in metaColliders)
                            {
                                if (String.Compare(s, m, StringComparison.Ordinal) != 0 && outlist.ContainsKey(m) == true)
                                    outlist.Remove(s);
                            }

                            break;
                        }
                    }
                }
            }

            if(savePrivate)
            {
                options = new CommandLineArgsOptions(optionsPrefix, optionsCompositeSeparator, outlist); // salva la nuova lista
                return null;
            }

            return outlist;
        }

        /// <summary>
        /// Controlla se una determinata opzione e' stata specificata (e quindi "attivata") dall'utente
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public bool IsOptionEnabled(string option)
        {
            if (options == null)
                return false;

            return options.IsOptionEnabled(option);
        }

        /// <summary>
        /// Controlla se almeno una delle opzioni specificate e' attiva
        /// </summary>
        /// <param name="optionsToCheck"></param>
        /// <returns></returns>
        public bool IsAnyOptionEnabled(IEnumerable<string> optionsToCheck)
        {
            if (options == null)
                return false;

            return options.IsAnyOptionEnabled(optionsToCheck);
        }

        /// <summary>
        /// Controlla se tutte le opzioni specificate sono attive o meno
        /// </summary>
        /// <param name="optionsToCheck"></param>
        /// <returns></returns>
        public bool AreAllOptionsEnabled(IEnumerable<string> optionsToCheck)
        {
            if (options == null)
                return false;

            return options.AreAllOptionsEnabled(optionsToCheck);
        }

        /// <summary>
        /// restituisce il valore di una opzione attivata
        /// se l'opzione non e' attiva, viene restituito null
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetOptionValue(string option)
        {
            if (options == null)
                return null;

            return options.GetValue(option);
        }

        /// <summary>
        /// ricostruisce la linea di comando
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public string Assemble(CommandLineArgsAssembleOrder order)
        {
            return Assemble(order, null);
        }

        /// <summary>
        /// ricostruisce la linea di comando
        /// </summary>
        /// <param name="order"></param>
        /// <param name="collisionRules"></param>
        /// <returns></returns>
        public string Assemble(CommandLineArgsAssembleOrder order, string[]collisionRules)
        {
            StringBuilder sbOpts = new StringBuilder();
            StringBuilder sbArgs = new StringBuilder();

            Dictionary<string, string> assembleOpts =
                (collisionRules != null && collisionRules.Length > 0)
                ? FixListCollisions(collisionRules, false)
                : options.ToDictionary();

            foreach (KeyValuePair<string, string> ovp in assembleOpts)
            {
                bool pad = sbOpts.Length>0;
                string optString = String.Format("{0}{1}{2}{3}",
                                            optionsPrefix,
                                            ovp.Key,
                                            (!String.IsNullOrEmpty(ovp.Value)) ? optionsCompositeSeparator.ToString() : String.Empty,
                                            ovp.Value);

                sbOpts.AppendFormat("{0}{1}", (pad) ? " " : String.Empty, optString);
            }

            foreach (KeyValuePair<int, string> avp in arguments)
                sbArgs.AppendFormat("{0}{1}", (avp.Key>0) ? " " : String.Empty, avp.Value);

            if (order == CommandLineArgsAssembleOrder.ArgumentsThenOptions)
                return String.Format("{0} {1}", sbArgs.ToString(), sbOpts.ToString());
            else
                return String.Format("{0} {1}", sbOpts.ToString(), sbArgs.ToString());
        }

        /// <summary>
        /// restituisce la lista delle opzioni pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except"></param>
        /// <returns></returns>
        public CommandLineArgsOptions OptionsExcept(IEnumerable<string> except)
        {
            return new CommandLineArgsOptions(
                            optionsPrefix,
                            optionsCompositeSeparator,
                            this.options.ToDictionary().Where(p => !except.Contains(p.Key)).ToDictionary(x => x.Key, x => x.Value));
        }

        /// <summary>
        /// restituisce la lista degli argomenti pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except"></param>
        /// <returns></returns>
        public CommandLineArgsOptions OptionsExcept(params string[] except)
        {
            return OptionsExcept(except.AsEnumerable());
        }

        /// <summary>
        /// restituisce la lista degli argomenti pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except">elenco di id chiavi da scartare</param>
        /// <returns>elenco di strighe</returns>
        public IEnumerable<string> ArgumentsExcept(IEnumerable<int> except)
        {
            List<string> newArgs = new List<string>();

            foreach (KeyValuePair<int, string> kvp in this.arguments.Where(p => !except.Contains(p.Key)))
                newArgs.Add(kvp.Value);

            return newArgs;
        }

        /// <summary>
        /// restituisce la lista degli argomenti pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except">elenco di argomenti da scartare</param>
        /// <returns>elenco di strighe</returns>
        public IEnumerable<string> ArgumentsExcept(params int[] except)
        {
            return ArgumentsExcept(except.AsEnumerable());
        }

        /// <summary>
        /// restituisce la lista degli argomenti pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except">elenco di argomenti da scartare</param>
        /// <returns>elenco di strighe</returns>
        public IEnumerable<string> ArgumentsExcept(IEnumerable<string> except)
        {
            List<string> newArgs = new List<string>();

            foreach (KeyValuePair<int, string> kvp in this.arguments.Where(p => !except.Contains(p.Value)))
                newArgs.Add(kvp.Value);

            return newArgs;
        }

        /// <summary>
        /// restituisce la lista degli argomenti pulita dalle eccezioni specificate
        /// </summary>
        /// <param name="except">elenco di argomenti da scartare</param>
        /// <returns>elenco di strighe</returns>
        public IEnumerable<string> ArgumentsExcept(params string[] except)
        {
            return ArgumentsExcept(except.AsEnumerable());
        }
    }
}