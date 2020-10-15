using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Input;
using UnderPDF.Helpers;
using UnderPDF.Interfaces;
using UnderPDF.Properties;
using ZXing;
using ZXing.QrCode;
using Image = System.Drawing.Image;

namespace UnderPDF.ViewModels
{
    class HomeScreenViewModel : BaseViewModel
    {
        string _downloadURL;

        string _decksFolderPath;
        string _decksFolder;
        string _decksFolderFullPath;

        string _deckFile;
        string _deckFileExtension;

        string _decklistsFolderPath;
        string _decklistsFolder;
        string _decklistsFolderFullPath;

        string _decklistFile;
        string _decklistFileExtension;

        string _cardsFolderPath;
        string _cardsFolder;
        string _cardsFolderFullPath;

        string _cardFileExtension;

        string _warbandsFolderPath;
        string _warbandsFolder;
        string _warbandsFolderFullPath;

        string _warbandFileExtension;

        string _fightersFolderPath;
        string _fightersFolder;
        string _fightersFolderFullPath;

        string _fighterFileExtension;

        int _maxFighters;

        float _heightInPoints;
        float _widthInPoints;

        private string _underworldsDBFilePath;
        private string _underworldsDBFile;
        private string _underworldsDBFileFullPath;

        private ObservableCollection<string> _logList = new ObservableCollection<string>();
        public ObservableCollection<string> LogList
        {
            get
            {
                return _logList;
            }
            set
            {
                if (value != _logList)
                {
                    _logList = value;
                    NotifyPropertyChanged(nameof(LogList));
                }
            }
        }

        private ObservableCollection<Card> _cardList = new ObservableCollection<Card>();
        public ObservableCollection<Card> CardList
        {
            get
            {
                return _cardList;
            }
            set
            {
                if (value != _cardList)
                {
                    _cardList = value;
                    NotifyPropertyChanged(nameof(CardList));
                }
            }
        }

        private ObservableCollection<Card> _filteredCardList = new ObservableCollection<Card>();
        public ObservableCollection<Card> FilteredCardList
        {
            get
            {
                return _filteredCardList;
            }
            set
            {
                if (value != _filteredCardList)
                {
                    _filteredCardList = value;
                    NotifyPropertyChanged(nameof(FilteredCardList));
                }
            }
        }

        private ObservableCollection<Card> _deckList = new ObservableCollection<Card>();
        public ObservableCollection<Card> DeckList
        {
            get
            {
                return _deckList;
            }
            set
            {
                if (value != _deckList)
                {
                    _deckList = value;
                    NotifyPropertyChanged(nameof(DeckList));
                }
            }
        }

        private ObservableCollection<string> _warbandList = new ObservableCollection<string>();
        public ObservableCollection<string> WarbandList
        {
            get
            {
                return _warbandList;
            }
            set
            {
                if (value != _warbandList)
                {
                    _warbandList = value;
                    NotifyPropertyChanged(nameof(WarbandList));
                }
            }
        }

        private readonly WebClient _webDownloader = new WebClient();

        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();

        private int _backgroundWorkerTaskId = 0;

        private string _cardFilter = "";
        public string CardFilter
        {
            get
            {
                return _cardFilter;
            }
            set
            {
                if (value != _cardFilter)
                {
                    _cardFilter = value;
                    FilterCardList(value);
                    NotifyPropertyChanged(nameof(CardFilter));
                }
            }
        }

        private string _deckName = "";
        public string DeckName
        {
            get
            {
                return _deckName;
            }
            set
            {
                if (value != _deckName)
                {
                    _deckName = value;
                    NotifyPropertyChanged(nameof(DeckName));
                }
            }
        }

        private int _selectedWarbandIndex = 0;
        public int SelectedWarbandIndex
        {
            get
            {
                return _selectedWarbandIndex;
            }
            set
            {
                if (value != _selectedWarbandIndex)
                {
                    _selectedWarbandIndex = value;
                    NotifyPropertyChanged(nameof(SelectedWarbandIndex));
                }
            }
        }

        private int _warbandCount = 0;
        public int WarbandCount
        {
            get
            {
                return _warbandCount;
            }
            set
            {
                if (value != _warbandCount)
                {
                    _warbandCount = value;
                    NotifyPropertyChanged(nameof(WarbandCount));
                }
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    NotifyPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public HomeScreenViewModel(IChangeViewModel viewModelChanger) : base(viewModelChanger)
        {
            AddToLog("UnderPDF started");
            Init();
        }

        public ICommand ImportCardsFromStringCommand
        {
            get { return new RelayCommand<string>(cards => ImportCardsFromString(cards)); }
        }

        public ICommand GenerateDeckPDFCommand
        {
            get { return new RelayCommand(GenerateDeckPDF); }
        }

        public ICommand GenerateDecklistCommand
        {
            get { return new RelayCommand(GenerateDecklist); }
        }

        public ICommand GenerateWarbandPDFCommand
        {
            get { return new RelayCommand(GenerateWarbandPDF); }
        }

        public ICommand AddCardToDeckListCommand
        {
            get { return new RelayCommand<Card>(selectedCard => AddCardToDeckList(selectedCard)); }
        }

        public ICommand RemoveCardFromDeckListCommand
        {
            get { return new RelayCommand<Card>(selectedCard => RemoveCardFromDeckList(selectedCard)); }
        }

        public ICommand AddAllCardsToDeckListCommand
        {
            get { return new RelayCommand(AddAllCardsToDeckList); }
        }

        public ICommand RemoveAllCardsFromDeckListCommand
        {
            get { return new RelayCommand(RemoveAllCardsFromDeckList); }
        }

        private void Init()
        {
            AddToLog("Initializing...");

            _backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);

            _heightInPoints = 249.449f;
            _widthInPoints = 178.583f;

            _downloadURL = @"https://www.underworldsdb.com/cards/";

            AddToLog("Download URL: " + _downloadURL);

            _decksFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _decksFolder = "decks";
            _decksFolderFullPath = _decksFolderPath + _decksFolder + "\\";

            AddToLog("Decks folder: " + _decksFolderFullPath);

            _deckFile = "Deck";
            _deckFileExtension = ".pdf";

            _decklistsFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _decklistsFolder = "decklists";
            _decklistsFolderFullPath = _decklistsFolderPath + _decklistsFolder + "\\";

            AddToLog("Decklists folder: " + _decklistsFolderFullPath);

            _decklistFile = "Decklist";
            _decklistFileExtension = ".pdf";

            _cardsFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _cardsFolder = "cards";
            _cardsFolderFullPath = _cardsFolderPath + _cardsFolder + "\\";

            AddToLog("Cards folder: " + _cardsFolderFullPath);

            _cardFileExtension = ".png";

            _fightersFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _fightersFolder = "fighters";
            _fightersFolderFullPath = _fightersFolderPath + _fightersFolder + "\\";

            AddToLog("Fighters folder: " + _fightersFolderFullPath);

            _fighterFileExtension = ".png";

            _maxFighters = 9;

            _warbandsFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            _warbandsFolder = "warbands";
            _warbandsFolderFullPath = _warbandsFolderPath + _warbandsFolder + "\\";

            AddToLog("Warbands folder: " + _warbandsFolderFullPath);

            _warbandFileExtension = ".pdf";

            _underworldsDBFilePath = AppDomain.CurrentDomain.BaseDirectory;
            _underworldsDBFile = "UnderworldsDB.csv";
            _underworldsDBFileFullPath = _underworldsDBFilePath + _underworldsDBFile;

            AddToLog("UnderworldsDB file: " + _underworldsDBFileFullPath);

            AddToLog("Reading UnderworldsDB file...");
            CardList = new ObservableCollection<Card>(File.ReadAllLines(_underworldsDBFileFullPath).Skip(1).Select(v => Card.FromCsv(v)).ToList());

            List<Card> cardDeleteList = CardList.Where(card => string.IsNullOrEmpty(card.Name)).ToList();

            foreach (Card card in cardDeleteList)
            {
                CardList.Remove(card);
            }

            foreach (Card currentCard in CardList)
            {
                string oldWarband = WarbandList.Where(x => x == currentCard.Faction).FirstOrDefault();

                if (oldWarband == null && currentCard.Faction != "Universal")
                {
                    WarbandCount++;
                    WarbandList.Add(currentCard.Faction);
                }
            }

            WarbandList = new ObservableCollection<string>(WarbandList.OrderBy(i => i));

            WarbandList.Insert(0, "-- Select Warband --");
            WarbandList.Insert(1, "-- All Warbands --");

            AddToLog("Finished reading UnderWorldsDB file: " + CardList.Count.ToString() + " cards / " + WarbandCount.ToString() + " warbands");

            AddToLog("Initialized");

            FilteredCardList = CardList;
        }

        private void GenerateDeckPDF()
        {
            if (DeckList.Count > 0)
            {
                AddToLog("Generating Deck PDF...");
                _backgroundWorkerTaskId = 0;
                _backgroundWorker.RunWorkerAsync();
            }
        }

        private void ImportCardsFromString(string cards)
        {
            DeckList.Clear();

            List<string> cardNumberList;


            if (Uri.TryCreate(cards, UriKind.Absolute, out Uri uri))
            {
                cards = HttpUtility.ParseQueryString(uri.Query).Get("deck");
                DeckName = HttpUtility.ParseQueryString(uri.Query).Get("deckname");
            }



            cardNumberList = cards.Split(',').ToList();

            foreach (string currentCardNumber in cardNumberList)
            {
                Card newCard = CardList.Where(x => x.Number == currentCardNumber).FirstOrDefault();
                if (newCard != null)
                {
                    DeckList.Add(newCard);
                }
            }
        }

        private string GetPurifiedCardName(Card card)
        {
            string purifiedCardName;

            purifiedCardName = card.Name;

            purifiedCardName = Regex.Replace(purifiedCardName, "[!,'?]", "");
            purifiedCardName = Regex.Replace(purifiedCardName, "[ ]", "-");

            return purifiedCardName;
        }

        private string GetCardDownloadLink(Card card)
        {
            string downloadLink = "";
           
            downloadLink += _downloadURL + card.Release + "/";
            downloadLink += GetPurifiedCardName(card);
            downloadLink += _cardFileExtension;

            return downloadLink;
        }

        private void DownloadCards()
        {
            int cardSuccess = 0;
            int cardError = 0;
            int cardSkipped = 0;
            int cardProgress = 0;

            AddToLog("Step 1/2 - Downloading cards...");
            foreach (Card currentCard in DeckList)
            {
                cardProgress++;

                AddToLog("Card " + cardProgress.ToString() + "/" + DeckList.Count.ToString() + " - (" + currentCard.Number + ") " + currentCard.Name);

                string cardFolderFullPath = _cardsFolderFullPath + currentCard.Release + "\\";
                string cardFileName = GetPurifiedCardName(currentCard) + _cardFileExtension;
                string cardFileFullPath = cardFolderFullPath + cardFileName;
                
                if (!Directory.Exists(cardFolderFullPath))
                {
                    Directory.CreateDirectory(cardFolderFullPath);
                }

                if (!File.Exists(cardFileFullPath))
                {
                    try
                    {
                        _webDownloader.DownloadFile(GetCardDownloadLink(currentCard), cardFileFullPath);
                        AddToLog("...Success!");
                        cardSuccess++;
                    }
                    catch
                    {
                        AddToLog("...Error!");
                        cardError++;
                    }
                }
                else
                {
                    AddToLog("...Skipped!");
                    cardSkipped++;
                }
            }

            AddToLog(cardSuccess + " downloaded");
            AddToLog(cardSkipped + " skipped");
            AddToLog(cardError + " failed");

            AddToLog("Step 1/2 - Finished");         
        }

        private void CreateDeckPDF()
        {
            int cardSuccess = 0;
            int cardError = 0;
            int cardProgress = 0;

            AddToLog("Step 2/2 - Creating PDF...");

            if (!Directory.Exists(_decksFolderFullPath))
            {
                Directory.CreateDirectory(_decksFolderFullPath);
            }

            string datePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_");

            string deckFileName;

            if (DeckName.Length > 0)
            {
                deckFileName = datePrefix + DeckName + _deckFileExtension;
            }
            else
            {
                deckFileName = datePrefix + _deckFile + _deckFileExtension;
            }

            string deckFileFullPath = _decksFolderFullPath + deckFileName;

            AddToLog("PDF file: " + deckFileFullPath);

            AddToLog("Creating file...");
            PdfWriter pdfWriter = new PdfWriter(deckFileFullPath);
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            Document document = new Document(pdfDocument, PageSize.A4);
            document.SetMargins(20, 20, 20, 20);

            AddToLog("Creating layout...");
            Table table = new Table(3, false);
            table.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);

            AddToLog("Inserting cards...");
            foreach (Card currentCard in DeckList)
            {
                cardProgress++;

                AddToLog("Card " + cardProgress.ToString() + "/" + DeckList.Count.ToString() + " - (" + currentCard.Number + ") " + currentCard.Name);

                try
                {
                    string cardFolderFullPath = _cardsFolderFullPath + currentCard.Release + "\\";
                    string cardFileName = GetPurifiedCardName(currentCard) + _cardFileExtension;
                    string cardFileFullPath = cardFolderFullPath + cardFileName;

                    iText.Layout.Element.Image image = new iText.Layout.Element.Image(ImageDataFactory.Create(cardFileFullPath));
                    image.ScaleAbsolute(_widthInPoints, _heightInPoints);

                    Cell cell = new Cell();
                    cell.Add(image);
                    cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                    cell.SetPaddings(0, 1, 1, 0);
                     
                    table.AddCell(cell);
                    cardSuccess++;
                    AddToLog("...Success!");
                }
                catch
                {
                    cardError++;
                    AddToLog("...Error!");
                }
            }

            document.Add(table);

            AddToLog(cardSuccess + " inserted");
            AddToLog(cardError + " failed");

            AddToLog("Finalizing...");

            document.Close();

            AddToLog("Step 2/2 - Finished");

            pdfWriter.Close();
        }

        private void GenerateWarbandPDF()
        {
            if (SelectedWarbandIndex > 0)
            {
                AddToLog("Generating Warband PDF...");
                _backgroundWorkerTaskId = 1;
                _backgroundWorker.RunWorkerAsync();
            }
        }

        private string GetPurifiedWarbandName(string warbandName)
        {
            string purifiedWarbandName;

            purifiedWarbandName = warbandName;

            purifiedWarbandName = Regex.Replace(purifiedWarbandName, "[!,'?’]", "");
            purifiedWarbandName = Regex.Replace(purifiedWarbandName, "[ ]", "-");

            return purifiedWarbandName;
        }

        private string GetFighterDownloadLink(string warbandName, int fighterNumber, bool isInspired)
        {
            string downloadLink = "";

            downloadLink += _downloadURL + "fighters" + "/";
            downloadLink += GetPurifiedWarbandName(warbandName) + "-" + fighterNumber.ToString();

            if(isInspired)
            {
                downloadLink += "-inspired";
            }

            downloadLink += _fighterFileExtension;

            return downloadLink;
        }

        private void DownloadFighters()
        {
            int warbandsToDownload = 1;
            int warbandProgress = 0;
   
            AddToLog("Step 1/2 - Downloading fighters...");

            if(SelectedWarbandIndex == 1)
            {
                warbandsToDownload = WarbandList.Count() - 2;
            }

            for (int i = 2; i < warbandsToDownload + 2; i++)
            {
                warbandProgress++;

                string warbandName;

                int fighterSuccess = 0;
                int fighterSkipped = 0;

                bool isInspired = false;

                if(SelectedWarbandIndex == 1)
                {
                    warbandName = WarbandList[i];
                    AddToLog("Warband " + warbandProgress.ToString() + "/" + warbandsToDownload.ToString() + " - " + warbandName);
                }
                else
                {
                    warbandName = WarbandList[SelectedWarbandIndex];
                    AddToLog("Warband - " + warbandName);
                }
  
                for (int currentFighter = 1; currentFighter <= _maxFighters; currentFighter++)
                {
                    if (!isInspired)
                    {
                        AddToLog("Fighter " + currentFighter.ToString() + " - Normal");
                    }
                    else
                    {
                        AddToLog("Fighter " + currentFighter.ToString() + " - Inspired");
                    }

                    string fighterFolderFullPath = _fightersFolderFullPath + warbandName + "\\";
                    string fighterFileName = GetPurifiedWarbandName(warbandName) + "-" + currentFighter.ToString();

                    if (isInspired)
                    {
                        fighterFileName += "-inspired";
                    }

                    fighterFileName += _fighterFileExtension;

                    string fighterFileFullPath = fighterFolderFullPath + fighterFileName;

                    if (!Directory.Exists(fighterFolderFullPath))
                    {
                        Directory.CreateDirectory(fighterFolderFullPath);
                    }

                    if (!File.Exists(fighterFileFullPath))
                    {
                        try
                        {
                            _webDownloader.DownloadFile(GetFighterDownloadLink(warbandName, currentFighter, isInspired), fighterFileFullPath);
                            fighterSuccess++;
                            AddToLog("...Success!");
                        }
                        catch
                        {
                            AddToLog("...doesn't exist. Aborting!");
                            break;
                        }
                    }
                    else
                    {
                        AddToLog("...Skipped!");
                        fighterSkipped++;
                    }

                    if (!isInspired)
                    {
                        currentFighter--;
                    }

                    isInspired = !isInspired;
                }

                AddToLog(fighterSuccess + " downloaded");
                AddToLog(fighterSkipped + " skipped");
            }

            AddToLog("Step 1/2 - Finished");
        }

        private void CreateWarbandPDF()
        {
            int warbandsToCreate = 1;
            int warbandProgress = 0;

          
            AddToLog("Step 2/2 - Creating PDF...");

            if (SelectedWarbandIndex == 1)
            {
                warbandsToCreate = WarbandList.Count() - 2;
            }

            if (!Directory.Exists(_warbandsFolderFullPath))
            {
                Directory.CreateDirectory(_warbandsFolderFullPath);
            }

            for (int i = 2; i < warbandsToCreate + 2; i++)
            {
                warbandProgress++;

                string warbandName;

                int fighterSuccess = 0;

                bool isInspired = false;

                if (SelectedWarbandIndex == 1)
                {
                    warbandName = WarbandList[i];
                    AddToLog("Warband " + warbandProgress.ToString() + "/" + warbandsToCreate.ToString() + " - " + warbandName);
                }
                else
                {
                    warbandName = WarbandList[SelectedWarbandIndex];
                    AddToLog("Warband - " + warbandName);
                }

                string warbandFileName = warbandName + _warbandFileExtension;
                string warbandFileFullPath = _warbandsFolderFullPath + warbandFileName;

                AddToLog("PDF file: " + warbandFileFullPath);

                AddToLog("Creating file...");
                PdfWriter pdfWriter = new PdfWriter(warbandFileFullPath);
                PdfDocument pdfDocument = new PdfDocument(pdfWriter);
                Document document = new Document(pdfDocument, PageSize.A4);
                document.SetMargins(20, 20, 20, 20);

                AddToLog("Creating layout...");
                Table table = new Table(3, false);
                table.SetHorizontalAlignment(HorizontalAlignment.CENTER);

                AddToLog("Inserting fighters...");

                for (int currentFighter = 1; currentFighter <= _maxFighters; currentFighter++)
                {
                    if (!isInspired)
                    {
                        AddToLog("Fighter " + currentFighter.ToString() + " - Normal");
                    }
                    else
                    {
                        AddToLog("Fighter " + currentFighter.ToString() + " - Inspired");
                    }

                    string fighterFolderFullPath = _fightersFolderFullPath + warbandName + "\\";
                    string fighterFileName = GetPurifiedWarbandName(warbandName) + "-" + currentFighter.ToString();

                    if (isInspired)
                    {
                        fighterFileName += "-inspired";
                    }

                    fighterFileName += _fighterFileExtension;

                    string fighterFileFullPath = fighterFolderFullPath + fighterFileName;
             
                    try
                    {
                        iText.Layout.Element.Image image = new iText.Layout.Element.Image(ImageDataFactory.Create(fighterFileFullPath));
                        image.ScaleAbsolute(_widthInPoints, _heightInPoints);

                        Cell cell = new Cell();
                        cell.Add(image);
                        cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        cell.SetPaddings(0, 1, 1, 0);

                        table.AddCell(cell);
                        fighterSuccess++;
                        AddToLog("...Success!");
                    }
                    catch
                    {
                        AddToLog("...doesn't exist. Aborting!");
                        break;
                    }

                    if (!isInspired)
                    {
                        currentFighter--;
                    }

                    isInspired = !isInspired;
                }

                document.Add(table);

                AddToLog(fighterSuccess + " inserted");

                AddToLog("Finalizing...");

                document.Close();

                AddToLog("Step 2/2 - Finished");

                pdfWriter.Close();
            }
        }

        private Cell createCell(string content, TextAlignment alignment, PdfFont font, bool isCard, bool championship, bool restricted, bool surge)
        {
            Paragraph p = new Paragraph();

            if (isCard)
            {

                Image img;

                iText.Layout.Element.Image image;

                ImageData rawImage;

                if (championship)
                {
                    img = (Image)Resources.ResourceManager.GetObject("res_trophy_green");
                }
                else
                {
                    img = (Image)Resources.ResourceManager.GetObject("res_trophy_red");
                }

                using(MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);

                    rawImage = ImageDataFactory.Create(ms.ToArray());
                }
                
                image = new iText.Layout.Element.Image(rawImage);

                
                p.Add(image);
                p.Add(" ");

                p.Add(new Text(content));

                if (surge)
                {

                    img = (Image)Resources.ResourceManager.GetObject("res_lightning");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);

                        rawImage = ImageDataFactory.Create(ms.ToArray());
                    }

                    image = new iText.Layout.Element.Image(rawImage);
     

                    p.Add(" ");
                    p.Add(image);

                }

                if (restricted)
                {

                    img = (Image)Resources.ResourceManager.GetObject("res_lock");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);

                        rawImage = ImageDataFactory.Create(ms.ToArray());
                    }

                    image = new iText.Layout.Element.Image(rawImage);

                    p.Add(" ");
                    p.Add(image);

                }

            }
            else
            {
                p.Add(new Text(content));
            }

            Cell cell = new Cell(1, 1).Add(p);

            cell.SetTextAlignment(alignment);
            cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            cell.SetFont(font);
            return cell;
        }

        private Cell createCell(iText.Layout.Element.Image content, HorizontalAlignment alignment, int colSpan)
        {
            content.SetHorizontalAlignment(alignment);
            Cell cell = new Cell(1, colSpan).Add(content);
            cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            return cell;
        }

        private void GenerateDecklist()
        {
            if (DeckList.Count > 0)
            {
                AddToLog("Generating Decklist PDF...");
                _backgroundWorkerTaskId = 2;
                _backgroundWorker.RunWorkerAsync();
            }  
        }

        private void CreateDecklistPDF()
        {
            AddToLog("Creating PDF...");

            List<Card> tempList = DeckList.ToList<Card>();

            string qrURL = "https://www.underworldsdb.com/shared.php?deck=0,";

            int objectiveCount = 0;
            int gambitCount = 0;
            int upgradeCount = 0;

            foreach (Card currentCard in tempList)
            {
                switch (currentCard.Type)
                {
                    case "Objective": objectiveCount++; break;
                    case "Ploy": gambitCount++; break;
                    case "Spell": gambitCount++; break;
                    case "Upgrade": upgradeCount++; break;
                }
            }

            int maxRows = Math.Max(objectiveCount, Math.Max(gambitCount, upgradeCount));
            maxRows = maxRows + 3;

            if (!Directory.Exists(_decklistsFolderFullPath))
            {
                Directory.CreateDirectory(_decklistsFolderFullPath);
            }

            string datePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_");
          
            string decklistFileName;
            if (DeckName.Length>0)
            {
                decklistFileName = datePrefix + DeckName + _decklistFileExtension;
            }
            else
            {
                decklistFileName = datePrefix + _decklistFile + _decklistFileExtension;
            }
 
            string decklistFileFullPath = _decklistsFolderFullPath + decklistFileName;

            AddToLog("PDF file: " + decklistFileFullPath);

            PdfWriter pdfWriter = new PdfWriter(decklistFileFullPath);
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            Document document = new Document(pdfDocument, PageSize.A4);
            document.SetMargins(20, 20, 20, 20);

            Table table = new Table(3);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            PdfFont regular = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            PdfFont bold = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            for (int row = 1; row <= maxRows; row++)
            {
                for (int col = 1; col <= 3; col++)
                {
                    if (row == 1 && col == 2)
                    {
                        if(DeckName.Length>0)
                        {
                            table.AddCell(createCell(DeckName, TextAlignment.CENTER, bold, false, false, false, false));
                        }
                        else
                        {
                            table.AddCell(createCell("Decklist", TextAlignment.CENTER, bold, false, false, false, false));
                        }
                        
                        continue;
                    }

                    if (row == 2 && col == 1)
                    {
                        table.AddCell(createCell("Objectives (" + objectiveCount.ToString() + ")", TextAlignment.CENTER, bold, false, false, false, false));
                        continue;
                    }

                    if (row == 2 && col == 2)
                    {
                        table.AddCell(createCell("Gambits (" + gambitCount.ToString() + ")", TextAlignment.CENTER, bold, false, false, false, false));
                        continue;
                    }

                    if (row == 2 && col == 3)
                    {
                        table.AddCell(createCell("Upgrades (" + upgradeCount.ToString() + ")", TextAlignment.CENTER, bold, false, false, false, false));
                        continue;
                    }

                    if (row > 2 && row < maxRows)
                    {
                        if (col == 1)
                        {
                            bool bFound = false;
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (tempList[i].Type == "Objective")
                                {
                                    bool championship = false;
                                    if (tempList[i].OrganisedPlay.Substring(0, 1) == "V")
                                    {
                                        championship = true;
                                    }
                                    bool restricted = false;
                                    if (tempList[i].Restricted == "Restricted")
                                    {
                                        restricted = true;
                                    }
                                    bool surge = false;
                                    if (tempList[i].ObjType == "Surge")
                                    {
                                        surge = true;
                                    }
                                    table.AddCell(createCell(tempList[i].Name, TextAlignment.LEFT, regular, true, championship, restricted, surge));
                                    qrURL += tempList[i].Number + ",";
                                    tempList.RemoveAt(i);
                                    bFound = true;
                                    break;
                                }
                            }
                            if (bFound)
                            {
                                continue;
                            }
                        }

                        if (col == 2)
                        {
                            bool bFound = false;
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (tempList[i].Type == "Ploy" || tempList[i].Type == "Spell")
                                {
                                    bool championship = false;
                                    if (tempList[i].OrganisedPlay.Substring(0, 1) == "V")
                                    {
                                        championship = true;
                                    }
                                    bool restricted = false;
                                    if (tempList[i].Restricted == "Restricted")
                                    {
                                        restricted = true;
                                    }
                                    table.AddCell(createCell(tempList[i].Name, TextAlignment.LEFT, regular, true, championship, restricted, false));
                                    qrURL += tempList[i].Number + ",";
                                    tempList.RemoveAt(i);
                                    bFound = true;
                                    break;
                                }
                            }
                            if (bFound)
                            {
                                continue;
                            }
                        }

                        if (col == 3)
                        {
                            bool bFound = false;
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                if (tempList[i].Type == "Upgrade")
                                {
                                    bool championship = false;
                                    if (tempList[i].OrganisedPlay.Substring(0, 1) == "V")
                                    {
                                        championship = true;
                                    }
                                    bool restricted = false;
                                    if (tempList[i].Restricted == "Restricted")
                                    {
                                        restricted = true;
                                    }
                                    table.AddCell(createCell(tempList[i].Name, TextAlignment.LEFT, regular, true, championship, restricted, false));
                                    qrURL += tempList[i].Number + ",";
                                    tempList.RemoveAt(i);
                                    bFound = true;
                                    break;
                                }
                            }
                            if (bFound)
                            {
                                continue;
                            }
                        }
                    }

                    if (row == maxRows && col == 1)
                    {
                        
                            QrCodeEncodingOptions options = new QrCodeEncodingOptions();

                            options = new QrCodeEncodingOptions
                            {
                                DisableECI = true,
                                CharacterSet = "UTF-8",
                                Width = 150,
                                Height = 150,

                            };

                            var writer = new BarcodeWriter();
                            writer.Format = BarcodeFormat.QR_CODE;
                            writer.Options = options;

                            qrURL = qrURL.Remove(qrURL.Length - 1, 1);

                            if(DeckName.Length>0)
                            {
                                qrURL += "&deckname=" + DeckName.Replace(" ","%20");
                            }

                        if (qrURL.Length <= 2953)
                        {
                            Image img = writer.Write(qrURL);

                            MemoryStream ms = new MemoryStream();

                            img.Save(ms, ImageFormat.Bmp);


                            ImageData rawImage = ImageDataFactory.Create(ms.ToArray());

                            iText.Layout.Element.Image image = new iText.Layout.Element.Image(rawImage);

                            table.AddCell(createCell(image, HorizontalAlignment.CENTER, 3));
                        }

                        break;
                    }

                    table.AddCell(createCell("", TextAlignment.CENTER, regular, false, false, false, false));
                }
            }


            document.Add(table);
            AddToLog("Finalizing...");
            document.Close();
            pdfWriter.Close();

            AddToLog("Finished");
        }

        private void FilterCardList(string filter)
        {
            FilteredCardList = new ObservableCollection<Card>(CardList.Where(g => g.Name.ToUpper().Contains(filter.ToUpper())).ToList());
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            IsBusy = true;

            Thread.Sleep(50);

            if (_backgroundWorkerTaskId == 0)
            {
                DownloadCards();
                CreateDeckPDF();
            }

            if(_backgroundWorkerTaskId == 1)
            {
                DownloadFighters();
                CreateWarbandPDF();
            }

            if(_backgroundWorkerTaskId == 2)
            {
                CreateDecklistPDF();
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
        }

        private void AddCardToDeckList(Card SelectedCard)
        {
            DeckList.Add(SelectedCard);
        }

        private void RemoveCardFromDeckList(Card SelectedCard)
        {
           DeckList.Remove(SelectedCard);
        }

        private void AddAllCardsToDeckList()
        {
            foreach(Card card in CardList)
            {
                DeckList.Add(card);
            }
        }

        private void RemoveAllCardsFromDeckList()
        {
            DeckList.Clear();
        }

        private void AddToLog(string logMessage)
        {
            string log = "";

            log += "[";
            log += DateTime.Now.ToString("HH:mm:ss.fff");
            log += "]";
            log += " ";
            log += logMessage;
       
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _logList.Add(log);
            }); 
        }
    }
}
