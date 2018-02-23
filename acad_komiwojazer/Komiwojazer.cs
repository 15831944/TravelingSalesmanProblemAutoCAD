using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;

namespace acad_komiwojazer
{
    public class Komiwojazer
    {
        // define command "helloworld"
        [CommandMethod("helloworld")]
        public void MyHelloWorld()
        {
            // get the editor object
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            // write to command line
            ed.WriteMessage("\nHello World!");
        } // MyHelloWorld

        // define command "kw" alias
        [CommandMethod("kw")]
        public void kw()
        {
            komiwojazer_panel();
        }

        // declare the palette set window object, this will only be created once
        public Autodesk.AutoCAD.Windows.PaletteSet myPaletteSet;
        // declare the palette which goes inside of the paletteset window, created only once
        public UserControl1 myPalette;

        // deklaracja tablicy z punktami pomiarowymi
        public List<Punkt3d> punkty;

        // define command "komiwojazer"
        [CommandMethod("komiwojazer")]
        public void komiwojazer_panel()
        {
            // check to make sure that the paletteset window is not already active
            if (myPaletteSet == null)
            {
                // create the paletteset window first
                myPaletteSet = new PaletteSet("Problem komiwojazera", new Guid("{B953F94A-6C3D-49ee-A07A-0A1F39FA2594}"));
                // create the palette window to go into the paletteset
                myPalette = new UserControl1();
                // now add the palette to the paletteset
                myPaletteSet.Add("Komiwojazer", myPalette);
            } // if (myPaletteSet == null)

            // double check that the window is actually displayed
            myPaletteSet.Visible = true;

            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Now start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                punkty = new List<Punkt3d>();

                // Step through the Block table record
                foreach (ObjectId asObjId in acBlkTblRec)
                {
                    if (asObjId.ObjectClass.Name == "AcDb3dSolid")
                    {
                        Solid3d acEnt = acTrans.GetObject(asObjId, OpenMode.ForWrite) as Solid3d;
                        Punkt3d p = new Punkt3d(acEnt.MassProperties.Centroid.X, acEnt.MassProperties.Centroid.Y, acEnt.MassProperties.Centroid.Z);
                        punkty.Add(p);

                        //acDoc.Editor.WriteMessage("\nName: " + acEnt.BlockName);
                        //acDoc.Editor.WriteMessage("\nObjectID: " + acEnt.ToString());
                        //acDoc.Editor.WriteMessage("\nCentroidPoint: (" + acEnt.MassProperties.Centroid.X + ", " + acEnt.MassProperties.Centroid.Y + ", " + acEnt.MassProperties.Centroid.Z + ")");
                        //acDoc.Editor.WriteMessage("\nHandle: " + asObjId.Handle.ToString());
                        //acDoc.Editor.WriteMessage("\n");
                    }
                }

                // Rozwiazanie problemu komiwojazera
                komiwojazer();

                // Returns the layer table for the current database
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                string sLayerName = "Komiwojazer";

                // Usuniecie obiektow z warstwy Komiwojazer
                // Step through the Block table record
                foreach (ObjectId asObjId in acBlkTblRec)
                {
                    Entity acEnt = acTrans.GetObject(asObjId, OpenMode.ForRead) as Entity;
                    if (acEnt.Layer == sLayerName)
                    {
                        acEnt.UpgradeOpen();
                        acEnt.Erase(true);
                    }
                }

                // Check to see if MyLayer exists in the Layer table
                if (acLyrTbl.Has(sLayerName) == true)
                {
                    try
                    {
                        LayerTableRecord acLyrTblRec;
                        acLyrTblRec = acTrans.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                        // Erase the unreferenced layer
                        acLyrTblRec.Erase(true);
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                    {
                        // Layer could not be deleted
                        Application.ShowAlertDialog("Error:\n" + Ex.Message);
                    }
                }

                // Check to see if MyLayer exists in the Layer table
                if (acLyrTbl.Has(sLayerName) != true)
                {
                    try
                    {
                        // Open the Layer Table for write
                        acLyrTbl.UpgradeOpen();
                        // Create a new layer table record and name the layer "MyLayer"
                        LayerTableRecord acLyrTblRec = new LayerTableRecord();

                        // Ustawienie wlasciwosci warstwy.
                        acLyrTblRec.Name = sLayerName;
                        acLyrTblRec.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 0);
                        acLyrTblRec.LineWeight = LineWeight.LineWeight020;

                        // Add the new layer table record to the layer table and the transaction
                        acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);

                        acBlkTblRec.UpgradeOpen();

                        // Dodanie linii pomiedzy punktem ostatnim i pierwszym
                        if (punkty.Count > 0)
                        {
                            Line acLine = new Line(new Point3d(punkty.Last().x, punkty.Last().y, punkty.Last().z), new Point3d(punkty[0].x, punkty[0].y, punkty[0].z));
                            acLine.Layer = sLayerName;

                            // Add the new object to the block table record and the transaction
                            acBlkTblRec.AppendEntity(acLine);
                            acTrans.AddNewlyCreatedDBObject(acLine, true);
                        }

                        // Dodanie pozostalych linii
                        for (int i = 0; i < punkty.Count - 1; i++)
                        {
                            Line acLine = new Line(new Point3d(punkty[i].x, punkty[i].y, punkty[i].z), new Point3d(punkty[i + 1].x, punkty[i + 1].y, punkty[i + 1].z));
                            acLine.Layer = sLayerName;

                            // Add the new object to the block table record and the transaction
                            acBlkTblRec.AppendEntity(acLine);
                            acTrans.AddNewlyCreatedDBObject(acLine, true);
                        }
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                    {
                        Application.ShowAlertDialog("Error:\n" + Ex.Message);
                    }
                }

                // Commit the changes
                acTrans.Commit();

                // Dispose of the transaction
            }
        }

        // Liczba miast
        private int liczba_miast;
        // Wielkosc populacji
        private const int wielkosc_populacji = 100;
        // Odleglosci miedzy miastami
        private double[,] macierz_odleglosci;

        // Funkcja rozwiazujaca problem komiwojazera
        private void komiwojazer()
        {
            // liczba miast
            liczba_miast = punkty.Count;

            if (liczba_miast > 0)
            {
                // Odleglosci miedzy miastami
                macierz_odleglosci = new double[liczba_miast, liczba_miast];

                // Random
                Random rnd = new Random();

                // Obliczenie odleglosci miedzy miastami:
                for (int i = 0; i < liczba_miast; i++)
                {
                    for (int j = 0; j < liczba_miast; j++)
                    {
                        macierz_odleglosci[i, j] = Punkt3d.obliczOdleglosc(punkty[i], punkty[j]);
                    }
                }

                // Calkowity dystans miedzy miastami u osobnikow w danej populacji
                double[] dystans = new double[wielkosc_populacji];

                // Populacja osobnikow
                int[,] populacja = new int[wielkosc_populacji, liczba_miast];

                // Przygotowanie populacji - wpisanie do wszystkich osobnikow po kolei miast
                for (int i = 0; i < wielkosc_populacji; i++)
                {
                    for (int j = 0; j < liczba_miast; j++)
                    {
                        populacja[i, j] = j;
                    }
                }

                // Utworzenie roznych osobnikow (permutacje + ewentualne mutacje osobnikow)
                for (int i = 0; i < wielkosc_populacji; i++)
                {
                    permutacje(ref populacja, i);
                    mutuj(ref populacja, i);
                    while (sprawdz(populacja, i))
                    {
                        mutuj(ref populacja, i);
                    }

                    dystans[i] = obliczOdleglosc(populacja, i);
                }

                // Posortowanie osobnikow wg dystansu "komiwojazera"
                // Tzn. posortowanie wg najlepiej dostosowanych osobnikow
                sort(ref populacja, ref dystans);

	            // Petla pokolen
	            for(int k = 0; k < 200; k++)
	            {
		            for(int i = wielkosc_populacji / 2; i < wielkosc_populacji; i += 2)
		            {
			            // Kopiowanie rodzicow
                        for (int j = 0; j < liczba_miast; j++)
                        {
                            populacja[i, j] = populacja[i - wielkosc_populacji / 2, j];
                            populacja[i + 1, j] = populacja[i + 1 - wielkosc_populacji / 2, j];
                        }
            			
			            // Krzyzowanie rodzicow
			            krzyzyjOsobniki(ref populacja, i, i + 1);

			            // Sprawdzenie czy nie powtarzaja sie osobniki, jesli tak wykonujemy mutacje
                        while (sprawdz(populacja, i))
                        {
                            mutuj(ref populacja, i);
                        }

                        while (sprawdz(populacja, i + 1))
                        {
                            mutuj(ref populacja, i + 1);
                        }
		            }
            		
		            // Obliczenie lacznej odleglosci dla wszystkich osobnikow
		            for(int i = wielkosc_populacji / 2; i < wielkosc_populacji; i++)
		            {
			            dystans[i] = obliczOdleglosc(populacja, i);
		            }
            		
		            // Posortowanie populacji od najlepszych do najslabszych osobnikow
		            sort(ref populacja, ref dystans);
            		
		            for(int i = 1; i < wielkosc_populacji; i++)
		            {
			            // Mutacje z prawdopodobienstwem wystapienia
			            if(rnd.Next(100) < 20)
			            {
				            mutuj(ref populacja, i);
			            }

			            while (sprawdz(populacja, i))
                        {
                            mutuj(ref populacja, i);
                        }
		            }
            		
		            for(int i = 1; i < wielkosc_populacji; i++)
		            {
                        dystans[i] = obliczOdleglosc(populacja, i);
		            }
		            sort(ref populacja, ref dystans); //sortowanie populacji
	            }
                myPalette.label4.Text = dystans[0].ToString();

                // Zamiana Listy punkty na poprawna kolejnosc (do wyswietlania)
                List<Punkt3d> punkty_sort = new List<Punkt3d>();
                for (int i = 0; i < liczba_miast; i++)
                {
                    punkty_sort.Add(punkty[populacja[0, i]]);
                }

                punkty = punkty_sort;
            }
        }

        private void permutacje(ref int[,] populacja, int osobnik)
        {
            Random rnd = new Random();
            int element, tmp;

            for (int i = 0; i < populacja.GetLength(1); i++)
            {
                element = rnd.Next(populacja.GetLength(1));
                tmp = populacja[osobnik, element];
                populacja[osobnik, element] = populacja[osobnik, i];
                populacja[osobnik, i] = tmp;
            }
        }

        private bool sprawdz(int[,] populacja, int osobnik)
        {
	        bool spr;
        	
	        // Wyszukiwanie takich samych osobnikow w populacji do n-tego elementu
	        for (int i = 0; i < osobnik; i++)
	        {
                spr = true;
		        // Porownanie z wszystkimi poprzedzajacymi osobnikami populacji
		        for (int j = 0; j < populacja.GetLength(1); j++)
		        {
			        // Jezeli sie rozni choc jeden element w kolejnosci miast (gen) to elementy sa rozne
			        if (populacja[i, j] != populacja[osobnik, j])
			        {
                        spr = false;
                        break;
			        }
		        }

		        // Jezeli znaleziono takie same osobniki
                if (spr)
		        {
			        return true;
		        }
	        }

	        return false;
        }

        // Mutacja
        private void mutuj(ref int[,] populacja, int osobnik)
        {
	        // Wylosowanie genów do zamiany
            Random rnd = new Random();
	        int a = rnd.Next(populacja.GetLength(1));
	        int b;
	        do
	        {
                b = rnd.Next(populacja.GetLength(1));
	        } while (b == a);

	        // Zamiana genow
            int tmp = populacja[osobnik, a];
            populacja[osobnik, a] = populacja[osobnik, b];
            populacja[osobnik, b] = tmp;
        }

        // Obliczenie długosci calej trasy komiwojazera u danego osobnika
        private double obliczOdleglosc(int[,] populacja, int osobnik)
        {
            // Odleglosc miedzy ostatnim i pierwszym miastem
            int miasto1 = populacja[osobnik, 0];
            int miasto2 = populacja[osobnik, populacja.GetLength(1) - 1];
            double odleglosc = macierz_odleglosci[miasto1, miasto2];

            // Sumowanie odleglosci miedzy kolejnymi miastami
            for (int i = 0; i < populacja.GetLength(1) - 1; i++)
            {
                miasto1 = populacja[osobnik, i];
                miasto2 = populacja[osobnik, i + 1];
                odleglosc += macierz_odleglosci[miasto1, miasto2];
            }

            return odleglosc;
        }

        // Sortowanie populacji wg calkowitej odleglosci
        private void sort(ref int[,] populacja, ref double[] odleglosci)
        { 
	        int tmp_idx;
	        double tmp_odleglosc;
	        int tmp_osobnik;

	        // Przeszukanie elementow dla calej populacji
	        for(int i = 0; i < populacja.GetLength(0); i++)
	        {
                tmp_idx = i;
		        // Wyszukanie minimalnej odleglosci u danego osobnika
		        for(int j = i + 1; j < wielkosc_populacji; j++)
		        {
                    if (odleglosci[j] < odleglosci[tmp_idx])
			        {
                        tmp_idx = j;
			        }
		        }

		        // Gdy znaleziono osobnika "bardziej przystosowanego" (mniejsza odleglosc)
                if (tmp_idx != i)
		        {
			        // Zamiana elementow w tablicy z odleglosciami
                    tmp_odleglosc = odleglosci[tmp_idx];
                    odleglosci[tmp_idx] = odleglosci[i];
                    odleglosci[i] = tmp_odleglosc;

			        // Zamiana elementow w tablicy z osobnikami
			        for(int j = 0; j < populacja.GetLength(1); j++)
			        {
                        tmp_osobnik = populacja[tmp_idx, j];
                        populacja[tmp_idx, j] = populacja[i, j];
                        populacja[i, j] = tmp_osobnik;
			        }
		        }
	        }
        }

        // Krzyżowanie populacji
        private void krzyzyjOsobniki(ref int[,] populacja, int osobnik1, int osobnik2)
        {
	        int a, b;
	        int tmp;
        	
	        // Wylosowanie miasta do zamiany (skrzyzowania)
            Random rnd = new Random();
	        b = rnd.Next(populacja.GetLength(1));
        	
	        // Krzyzowanie
	        // Przyklad:
	        // o: osobnik1 = [1 3 2 5 4]  osobnik2 = [2 4 1 3 5]
	        // x: Zamiana miast na 2 pozycji
	        // o: osobnik1 = [1 4 2 5 4]  osobnik2 = [2 3 1 3 5]
	        // x: wyszukanie elementu dla nastepnej wymiany, w tym przykladzie
	        //    (pozycja != 2 && element == 4), czyli element 5 (drugi element "4" u osobnik1)
	        //
	        // Kolejne etapy zamiany elementow
	        // o: osobnik1 = [1 4 2 5 5]  osobnik2 = [2 3 1 3 4] (zamiana 4 elementu)
	        // o: osobnik1 = [1 4 2 3 5]  osobnik2 = [2 3 1 5 4] (koniec: brak duplikatow)
	        do
	        {
		        a = b;
        		
		        // Zamiana miast u osobnikow (krzyzowanie)
                tmp = populacja[osobnik2, a];
		        populacja[osobnik2, a] = populacja[osobnik1, a];
                populacja[osobnik1, a] = tmp;

		        b = -1;

		        // Wybranie miasta do nastepnej zamiany
		        for(int i = 0; i < populacja.GetLength(1); i++)
		        {
			        if(populacja[osobnik1, i] == tmp && i != a)
			        {
				        b = i;
			        }
		        }
	        } while (b >= 0);
        }

    } // class

    // Klasa Punkt3d
    public class Punkt3d
    {
        public double x, y, z;
        public Punkt3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Obliczanie odleglosci miedzy dwoma punktami
        public static double obliczOdleglosc(Punkt3d p1, Punkt3d p2)
        {
            double odleglosc;
            odleglosc = Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2) + Math.Pow(p1.z - p2.z, 2));
            return odleglosc;
        }
    } // Klasa Punkt3d
} // namespace
