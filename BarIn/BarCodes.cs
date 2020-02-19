using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BarCodes
{
    /// <summary>
    /// Генерация QR кода
    /// </summary>
    public static class QRCode
    {

        /// <summary>
        /// Создание QR кода
        /// </summary>
        /// <param name="Bytes">Кодируемая последовательность</param>
        /// <param name="Version">Номер версии (определяет размер)</param>
        /// <param name="Level">Уровень коррекции</param>
        /// <param name="MaskCode">Код маски ("-1" - оптимальная)</param>
        /// <param name="Zoom">Размер квадратиков в пикселах</param>
        /// <returns>QR код</returns>
        public static Bitmap CreateQR(byte[] Bytes, int Version, CorrectionLevel Level, int MaskCode, int Zoom)
        {
            #region Проверка параметров
            if (Bytes == null) throw new ArgumentNullException(ErrorNullData);
            if ((Version < 1) || (Version > 40)) throw new Exception(ErrorBadVersion);
            if (((int)Level < 0) || ((int)Level > 3)) throw new Exception(ErrorBadLevel);
            if ((MaskCode < -1) || (MaskCode > 7)) throw new Exception(ErrorBadMask);
            if (Zoom < 1) throw new Exception(ErrorZoom);
            #endregion
            #region Получение конечного потока данных (byte[] Data)
            byte[] cBytes = CodingByte(Bytes, Version, Level);
            byte[][] bData = BlockCreate(cBytes, Version, Level);
            byte[] Data = DataStream(bData, Version, Level);
            #endregion
            #region Выбор маски
            bool[,] Mtx;
            if(MaskCode==-1)
            {
                Mtx = CreateMatrix(Data, Version, Level, 0);
                int r = RateMatrix(Mtx);
                for(int i=1;i<8;i++)
                {
                    bool[,] m = CreateMatrix(Data, Version, Level, i);
                    int n = RateMatrix(m);
                    if(n<r)
                    {
                        Mtx = m;
                        r = n;
                    }
                }
            }
            else
             Mtx = CreateMatrix(Data, Version, Level, MaskCode);
            #endregion
            #region Формирование рисунка
            int BarSize = Mtx.GetLength(0);
            Bitmap Result = new Bitmap((BarSize + 8) * Zoom, (BarSize + 8) * Zoom);
            Graphics g = Graphics.FromImage(Result);
            g.Clear(Color.White);
            for (int x = 0; x < BarSize; x++)
                for (int y = 0; y < BarSize; y++)                
                    g.FillRectangle((Mtx[x, y] ? Brushes.Black : Brushes.White), (4 + x) * Zoom, (4 + y) * Zoom, Zoom, Zoom);
            #endregion
            return Result;
        }

        /// <summary>
        /// Уровень коррекции
        /// </summary>
        public enum CorrectionLevel
        {
            /// <summary>
            /// Уровень L
            /// </summary>
            LevelL = 0,
            /// <summary>
            /// Уровень M
            /// </summary>
            LevelM = 1,
            /// <summary>
            /// Уровень Q
            /// </summary>
            LevelQ = 2,
            /// <summary>
            /// Уровень H
            /// </summary>
            LevelH = 3
        }

        #region Вспомогательные функции

        private static byte[] CodingByte(byte[] Bytes, int Version, CorrectionLevel Level)
        {
            #region Кодирование (Data)
            BitSequence Data = new BitSequence(Bytes);
            #endregion
            #region Добавление служебных полей (DataTmp)
            int MaxLength = MaxCapacity[(int)Level, Version - 1];
            BitSequence TypeCode = new BitSequence(new bool[] { false, true, false, false });
            byte[] BytesCount =
                (Version < 10
                ? new byte[] { (byte)(Bytes.Length & 0x00FF) }
                : new byte[] { (byte)((Bytes.Length & 0xFF00) >> 8), (byte)(Bytes.Length & 0x00FF) });
            BitSequence DataLength = new BitSequence(BytesCount);
            BitSequence ResultBits = TypeCode + DataLength + Data;
            if (ResultBits.Length > MaxLength)
                throw new Exception(ErrorLongData);
            byte[] DataTmp = ResultBits.ToByteArray();
            #endregion
            #region Дополнение (Result)
            byte[] Result = new byte[MaxLength / 8];
            DataTmp.CopyTo(Result, 0);
            bool f = true;
            for (int i = DataTmp.Length; i < Result.Length; i++)
            {
                Result[i] = (f ? (byte)0b11101100 : (byte)0b00010001);
                f = !f;
            }
            #endregion
            return Result;
        }
        private static byte[][] BlockCreate(byte[] Data, int Version, CorrectionLevel Level)
        {
            int nBlock = BlockCount[(int)Level, Version - 1];
            byte[][] Result = new byte[nBlock][];
            int size = Data.Length / nBlock;
            int md = (Data.Length % nBlock);
            int ind = 0;
            for (int i = 0; i < nBlock; i++)
            {
                Result[i] = new byte[(i >= nBlock - md ? size + 1 : size)];
                for (int j = 0; j < Result[i].Length; j++)
                {
                    Result[i][j] = Data[ind];
                    ind++;
                }
            }
            return Result;
        }
        private static byte[] Correct(byte[] Block, int Version, CorrectionLevel Level)
        {
            int CorrSize = CorrByteCount[(int)Level, Version - 1];
            byte[] tmp = new byte[Math.Max(CorrSize, Block.Length)];
            Block.CopyTo(tmp, 0);
            for (int i = Block.Length; i < tmp.Length; i++)
                tmp[i] = 0;
            for (int cnt = 0; cnt < Block.Length; cnt++)
            {
                byte a = tmp[0];
                for (int i = 1; i < tmp.Length; i++)
                    tmp[i - 1] = tmp[i];
                tmp[tmp.Length - 1] = 0;
                if (a != 0)
                {
                    byte b = TabGaluaDesc[a];
                    byte[] g = new byte[CorrSize];
                    for (int i = 0; i < g.Length; i++)
                        g[i] = (byte)((Polinom[CorrSize][i] + b) % 255);
                    for (int i = 0; i < g.Length; i++)
                        g[i] = TabGalua[g[i]];
                    for (int i = 0; i < g.Length; i++)
                        tmp[i] = (byte)(tmp[i] ^ g[i]);
                }
            }
            byte[] Result = new byte[CorrSize];
            for (int i = 0; i < CorrSize; i++)
                Result[i] = tmp[i];
            return Result;
        }
        private static byte[] DataStream(byte[][] Blocks, int Version, CorrectionLevel Level)
        {
            #region Создание блоков коррекции
            byte[][] CorrBlocks = new byte[Blocks.Length][];
            for (int i = 0; i < CorrBlocks.Length; i++)
                CorrBlocks[i] = Correct(Blocks[i], Version, Level);
            #endregion
            #region Объединение блоков
            List<byte> Result = new List<byte>();
            int MaxBlock = (from x in Blocks select x.Length).Max();
            for (int i = 0; i < MaxBlock; i++)
                for (int j = 0; j < Blocks.Length; j++)
                    if (i < Blocks[j].Length)
                        Result.Add(Blocks[j][i]);
            int MaxCorr = (from x in CorrBlocks select x.Length).Max();
            for (int i = 0; i < MaxCorr; i++)
                for (int j = 0; j < CorrBlocks.Length; j++)
                    if (i < CorrBlocks[j].Length)
                        Result.Add(CorrBlocks[j][i]);
            #endregion
            return Result.ToArray();
        }
        private static bool[,] CreateMatrix(byte[] Data, int Version, CorrectionLevel Level, int MaskCode)
        {
            #region Создание матрицы (bool?[,] Mtx)
            int BarSize = 17 + 4 * Version;
            bool?[,] Mtx = new bool?[BarSize, BarSize];
            for (int i = 0; i < BarSize; i++)
                for (int j = 0; j < BarSize; j++)
                    Mtx[i, j] = null;
            #endregion
            #region Поисковые узоры
            void ProcBox(int x, int y, int l, bool f)
            {
                for (int i = 0; i < l; i++)
                    for (int j = 0; j < l; j++)
                        Mtx[x + i, y + j] = f;
            }
            #region Большие квадраты
            void BigBox(int x, int y)
            {
                ProcBox(x, y, 7, true);
                ProcBox(x + 1, y + 1, 5, false);
                ProcBox(x + 2, y + 2, 3, true);
            }
            ProcBox(0, 0, 8, false);
            BigBox(0, 0);
            ProcBox(0, BarSize - 8, 8, false);
            BigBox(0, BarSize - 7);
            ProcBox(BarSize - 8, 0, 8, false);
            BigBox(BarSize - 7, 0);
            #endregion
            #region Малые квадраты
            void SmallBox(int x, int y)
            {
                ProcBox(x, y, 5, true);
                ProcBox(x + 1, y + 1, 3, false);
                ProcBox(x + 2, y + 2, 1, true);
            }
            for (int i = 0; i < AlignPosition[Version - 1].Length; i++)
                for (int j = 0; j < AlignPosition[Version - 1].Length; j++)
                {
                    int x = AlignPosition[Version - 1][i];
                    int y = AlignPosition[Version - 1][j];
                    if (Mtx[x, y] == null)
                        SmallBox(x - 2, y - 2);
                }
            #endregion
            #region Полоса
            bool LineFlag = true;
            for (int i = 6; i < BarSize; i++)
            {
                if (Mtx[6, i] == null) Mtx[6, i] = LineFlag;
                if (Mtx[i, 6] == null) Mtx[i, 6] = LineFlag;
                LineFlag = (!LineFlag);
            }
            #endregion
            #endregion
            #region Код версии
            if (Version > 6)
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 6; j++)
                    {
                        Mtx[j, BarSize - 11 + i] = VersionPattern[Version][i][j];
                        Mtx[BarSize - 11 + i, j] = VersionPattern[Version][i][j];
                    }

            #endregion
            #region Код маски и уровня
            BitSequence MaskPatt = MaskLevelPattern[MaskCode, (int)Level];
            Mtx[0, 8] = MaskPatt[0];
            Mtx[1, 8] = MaskPatt[1];
            Mtx[2, 8] = MaskPatt[2];
            Mtx[3, 8] = MaskPatt[3];
            Mtx[4, 8] = MaskPatt[4];
            Mtx[5, 8] = MaskPatt[5];
            Mtx[7, 8] = MaskPatt[6];
            Mtx[8, 8] = MaskPatt[7];
            Mtx[8, 7] = MaskPatt[8];
            Mtx[8, 5] = MaskPatt[9];
            Mtx[8, 4] = MaskPatt[10];
            Mtx[8, 3] = MaskPatt[11];
            Mtx[8, 2] = MaskPatt[12];
            Mtx[8, 1] = MaskPatt[13];
            Mtx[8, 0] = MaskPatt[14];
            Mtx[8, BarSize - 1] = MaskPatt[0];
            Mtx[8, BarSize - 2] = MaskPatt[1];
            Mtx[8, BarSize - 3] = MaskPatt[2];
            Mtx[8, BarSize - 4] = MaskPatt[3];
            Mtx[8, BarSize - 5] = MaskPatt[4];
            Mtx[8, BarSize - 6] = MaskPatt[5];
            Mtx[8, BarSize - 7] = MaskPatt[6];
            Mtx[8, BarSize - 8] = true;
            Mtx[BarSize - 8, 8] = MaskPatt[7];
            Mtx[BarSize - 7, 8] = MaskPatt[8];
            Mtx[BarSize - 6, 8] = MaskPatt[9];
            Mtx[BarSize - 5, 8] = MaskPatt[10];
            Mtx[BarSize - 4, 8] = MaskPatt[11];
            Mtx[BarSize - 3, 8] = MaskPatt[12];
            Mtx[BarSize - 2, 8] = MaskPatt[13];
            Mtx[BarSize - 1, 8] = MaskPatt[14];
            #endregion
            #region Запись данных
            BitSequence stm = new BitSequence(Data);
            int xx = BarSize - 1;
            int yy = BarSize - 1;
            bool dx = false;
            int dy = -1;
            for(int n=0; n<stm.Length;n++)
            {
                #region Выбор следующей ячейки
                while (Mtx[xx,yy]!=null)
                {
                    if (xx == 6)
                    {
                        xx--;
                        continue;
                    }
                    if (!dx)
                    {
                        xx--;
                        dx = (!dx);
                        continue;
                    }
                    if((yy+dy>=0)&&(yy+dy<=BarSize-1))
                    {
                        xx++;
                        yy+=dy;
                        dx = (!dx);
                        continue;
                    }
                    xx--;
                    dx = (!dx);
                    dy = -dy;                    
                }
                #endregion
                Mtx[xx, yy] = (stm[n] ^ MaskFunction[MaskCode](xx , yy ));
            }
            #endregion
            #region Дозаполнение пустого места
            for (int i = 0; i < BarSize; i++)
                for (int j = 0; j < BarSize; j++)
                    if (Mtx[i, j] == null)
                        Mtx[i, j] = MaskFunction[MaskCode](i + 1, j + 1);
            #endregion
            #region Формирование результата
            bool[,] Result = new bool[BarSize, BarSize];
            for (int i = 0; i < BarSize; i++)
                for (int j = 0; j < BarSize; j++)
                    Result[i, j] = (bool)Mtx[i, j];
            #endregion
            return Result;
        }
        private static int RateMatrix(bool[,] Matrix)
        {
            int Result = 0;
            int sz = Matrix.GetLength(0);
            #region Правило 1 (вертикаль)
            for (int i = 0; i < sz; i++)
            {
                bool? f = null;
                int l = 0;
                for (int j = 0; j < sz; j++)
                {
                    if (Matrix[i, j] != f)
                    {
                        if (l > 5) Result += l - 2;
                        l = 0;
                        f = Matrix[i, j];
                    }
                    l++;
                }
                if (l > 5) Result += l - 2;
            }
            #endregion
            #region Правило 1 (горизонталь)
            for (int i = 0; i < sz; i++)
            {
                bool? f = null;
                int l = 0;
                for (int j = 0; j < sz; j++)
                {
                    if (Matrix[j, i] != f)
                    {
                        if (l > 5) Result += l - 2;
                        l = 0;
                        f = Matrix[j, i];
                    }
                    l++;
                }
                if (l > 5) Result += l - 2;
            }
            #endregion
            #region Правило 2
            for (int i = 1; i < sz; i++)
                for (int j = 1; j < sz; j++)
                    if ((Matrix[i - 1, j - 1] == Matrix[i, j]) && (Matrix[i, j - 1] == Matrix[i, j]) && (Matrix[i - 1, j] == Matrix[i, j]))
                        Result += 2;
            #endregion
            #region Правило 3 (вертикаль)
            for(int i=0;i<sz;i++)
                for(int j=0;j<sz-6;j++)
                    if(Matrix[i,j] && !Matrix[i, j + 1] && !Matrix[i, j + 2] && !Matrix[i, j + 3] && !Matrix[i, j + 4] && Matrix[i, j + 5] && !Matrix[i, j + 6])
                    {
                        if (((j > 3) && !Matrix[i, j - 1] && !Matrix[i, j - 2] && !Matrix[i, j - 3] && !Matrix[i, j - 4])
                            || ((j < sz - 10) && !Matrix[i, j + 7] && !Matrix[i, j + 8] && !Matrix[i, j + 9] && !Matrix[i, j + 10]))
                            Result += 40;
                    }
            #endregion
            #region Правило 3 (горизонталь)
            for (int i = 0; i < sz; i++)
                for (int j = 0; j < sz - 6; j++)
                    if (Matrix[j, i] && !Matrix[j + 1, i] && !Matrix[j + 2, i] && !Matrix[j + 3, i] && !Matrix[j + 4, i] && Matrix[j + 5, i] && !Matrix[j + 6, i])
                    {
                        if (((j > 3) && !Matrix[j - 1, i] && !Matrix[j - 2, i] && !Matrix[j - 3, i] && !Matrix[j - 4, i])
                            || ((j < sz - 10) && !Matrix[j + 7, i] && !Matrix[j + 8, i] && !Matrix[j + 9, i] && !Matrix[j + 10, i]))
                            Result += 40;
                    }
            #endregion
            #region Правило 4
            int bl = 0;
            foreach (bool x in Matrix)
                if (x) bl++;
            Result += 2 * Math.Abs((int)(100.0 * bl / (sz * sz) - 50));
            #endregion
            return Result;
        }

        #endregion

        #region Справочные данные

        /// <summary>
        /// Максимальная вместимость (в битах) полезных данных в зависимости от уровня коррекции и версии
        /// </summary>
        private static readonly int[,] MaxCapacity = new int[4, 40]
        {
            { 152, 272, 440, 640, 864, 1088, 1248, 1552, 1856, 2192, 2592, 2960, 3424, 3688, 4184, 4712, 5176, 5768, 6360, 6888, 7456, 8048, 8752, 9392, 10208, 10960, 11744, 12248, 13048, 13880, 14744, 15640, 16568, 17528, 18448, 19472, 20528, 21616, 22496, 23648 },
            { 128, 224, 352, 512, 688, 864, 992, 1232, 1456, 1728, 2032, 2320, 2672, 2920, 3320, 3624, 4056, 4504, 5016, 5352, 5712, 6256, 6880, 7312, 8000, 8496, 9024, 9544, 10136, 10984, 11640, 12328, 13048, 13800, 14496, 15312, 15936, 16816, 17728, 18672 },
            { 104, 176, 272, 384, 496, 608, 704, 880, 1056, 1232, 1440, 1648, 1952, 2088, 2360, 2600, 2936, 3176, 3560, 3880, 4096, 4544, 4912, 5312, 5744, 6032, 6464, 6968, 7288, 7880, 8264, 8920, 9368, 9848, 10288, 10832, 11408, 12016, 12656, 13328 },
            { 72, 128, 208, 288, 368, 480, 528, 688, 800, 976, 1120, 1264, 1440, 1576, 1784, 2024, 2264, 2504, 2728, 3080, 3248, 3536, 3712, 4112, 4304, 4768, 5024, 5288, 5608, 5960, 6344, 6760, 7208, 7688, 7888, 8432, 8768, 9136, 9776, 10208 }
        };
        /// <summary>
        /// Количество блоков в зависимости от уровня коррекции и версии
        /// </summary>
        private static readonly int[,] BlockCount = new int[4, 40]
        {
            { 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
            { 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
            { 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
            { 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }
        };
        /// <summary>
        /// Количество байтов коррекции на один блок в зависимости от уровня коррекции и версии       
        /// </summary>
        private static readonly int[,] CorrByteCount = new int[4, 40]
        {
            { 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
            { 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }
        };
        /// <summary>
        /// Поле Галуа
        /// </summary>
        private static readonly byte[] TabGalua = new byte[256]
        {
            1, 2, 4, 8, 16, 32, 64, 128, 29, 58, 116, 232, 205, 135, 19, 38,
            76, 152, 45, 90, 180, 117, 234, 201, 143, 3, 6, 12, 24, 48, 96, 192,
            157, 39, 78, 156, 37, 74, 148, 53, 106, 212, 181, 119, 238, 193, 159, 35,
            70, 140, 5, 10, 20, 40, 80, 160, 93, 186, 105, 210, 185, 111, 222, 161,
            95, 190, 97, 194, 153, 47, 94, 188, 101, 202, 137, 15, 30, 60, 120, 240,
            253, 231, 211, 187, 107, 214, 177, 127, 254, 225, 223, 163, 91, 182, 113, 226,
            217, 175, 67, 134, 17, 34, 68, 136, 13, 26, 52, 104, 208, 189, 103, 206,
            129, 31, 62, 124, 248, 237, 199, 147, 59, 118, 236, 197, 151, 51, 102, 204,
            133, 23, 46, 92, 184, 109, 218, 169, 79, 158, 33, 66, 132, 21, 42, 84,
            168, 77, 154, 41, 82, 164, 85, 170, 73, 146, 57, 114, 228, 213, 183, 115,
            230, 209, 191, 99, 198, 145, 63, 126, 252, 229, 215, 179, 123, 246, 241, 255,
            227, 219, 171, 75, 150, 49, 98, 196, 149, 55, 110, 220, 165, 87, 174, 65,
            130, 25, 50, 100, 200, 141, 7, 14, 28, 56, 112, 224, 221, 167, 83, 166,
            81, 162, 89, 178, 121, 242, 249, 239, 195, 155, 43, 86, 172, 69, 138, 9,
            18, 36, 72, 144, 61, 122, 244, 245, 247, 243, 251, 235, 203, 139, 11, 22,
            44, 88, 176, 125, 250, 233, 207, 131, 27, 54, 108, 216, 173, 71, 142, 1
        };
        /// <summary>
        /// Обратное поле Галуа
        /// </summary>
        private static readonly byte[] TabGaluaDesc = new byte[256]
        {
            0x00, 0, 1, 25, 2, 50, 26, 198, 3, 223, 51, 238, 27, 104, 199, 75,
            4, 100, 224, 14, 52, 141, 239, 129, 28, 193, 105, 248, 200, 8, 76, 113,
            5, 138, 101, 47, 225, 36, 15, 33, 53, 147, 142, 218, 240, 18, 130, 69,
            29, 181, 194, 125, 106, 39, 249, 185, 201, 154, 9, 120, 77, 228, 114, 166,
            6, 191, 139, 98, 102, 221, 48, 253, 226, 152, 37, 179, 16, 145, 34, 136,
            54, 208, 148, 206, 143, 150, 219, 189, 241, 210, 19, 92, 131, 56, 70, 64,
            30, 66, 182, 163, 195, 72, 126, 110, 107, 58, 40, 84, 250, 133, 186, 61,
            202, 94, 155, 159, 10, 21, 121, 43, 78, 212, 229, 172, 115, 243, 167, 87,
            7, 112, 192, 247, 140, 128, 99, 13, 103, 74, 222, 237, 49, 197, 254, 24,
            227, 165, 153, 119, 38, 184, 180, 124, 17, 68, 146, 217, 35, 32, 137, 46,
            55, 63, 209, 91, 149, 188, 207, 205, 144, 135, 151, 178, 220, 252, 190, 97,
            242, 86, 211, 171, 20, 42, 93, 158, 132, 60, 57, 83, 71, 109, 65, 162,
            31, 45, 67, 216, 183, 123, 164, 118, 196, 23, 73, 236, 127, 12, 111, 246,
            108, 161, 59, 82, 41, 157, 85, 170, 251, 96, 134, 177, 187, 204, 62, 90,
            203, 89, 95, 176, 156, 169, 160, 81, 11, 245, 22, 235, 122, 117, 44, 215,
            79, 174, 213, 233, 230, 231, 173, 232, 116, 214, 244, 234, 168, 80, 88, 175
        };
        /// <summary>
        /// Генирирующие многочлены
        /// </summary>
        private static readonly Dictionary<int, byte[]> Polinom = new Dictionary<int, byte[]>()
        {
            {7, new byte[]{ 87, 229, 146, 149, 238, 102, 21 } },
            {10, new byte[]{ 251, 67, 46, 61, 118, 70, 64, 94, 32, 45} },
            {13, new byte[]{ 74, 152, 176, 100, 86, 100, 106, 104, 130, 218, 206, 140, 78 } },
            {15, new byte[]{ 8, 183, 61, 91, 202, 37, 51, 58, 58, 237, 140, 124, 5, 99, 105 } },
            {16, new byte[]{ 120, 104, 107, 109, 102, 161, 76, 3, 91, 191, 147, 169, 182, 194, 225, 120} },
            {17, new byte[]{ 43, 139, 206, 78, 43, 239, 123, 206, 214, 147, 24, 99, 150, 39, 243, 163, 136} },
            {18, new byte[]{ 215, 234, 158, 94, 184, 97, 118, 170, 79, 187, 152, 148, 252, 179, 5, 98, 96, 153} },
            {20, new byte[]{ 17, 60, 79, 50, 61, 163, 26, 187, 202, 180, 221, 225, 83, 239, 156, 164, 212, 212, 188, 190} },
            {22, new byte[]{ 210, 171, 247, 242, 93, 230, 14, 109, 221, 53, 200, 74, 8, 172, 98, 80, 219, 134, 160, 105, 165, 231} },
            {24, new byte[]{ 229, 121, 135, 48, 211, 117, 251, 126, 159, 180, 169, 152, 192, 226, 228, 218, 111, 0, 117, 232, 87, 96, 227, 21} },
            {26, new byte[]{ 173, 125, 158, 2, 103, 182, 118, 17, 145, 201, 111, 28, 165, 53, 161, 21, 245, 142, 13, 102, 48, 227, 153, 145, 218, 70} },
            {28, new byte[]{ 168, 223, 200, 104, 224, 234, 108, 180, 110, 190, 195, 147, 205, 27, 232, 201, 21, 43, 245, 87, 42, 195, 212, 119, 242, 37, 9, 123} },
            {30, new byte[]{ 41, 173, 145, 152, 216, 31, 179, 182, 50, 48, 110, 86, 239, 96, 222, 125, 42, 173, 226, 193, 224, 130, 156, 37, 251, 216, 238, 40, 192, 180} }
        };
        /// <summary>
        /// Расположение выравнивающих узоров
        /// </summary>
        private static readonly int[][] AlignPosition = new int[][]
        {
            new int[]{ },
            new int[]{ 18 },
            new int[]{ 22 },
            new int[]{ 26 },
            new int[]{ 30 },
            new int[]{ 34 },
            new int[]{ 6, 22, 38},
            new int[]{ 6, 24, 42 },
            new int[]{ 6, 26, 46 },
            new int[]{ 6, 28, 50 },
            new int[]{ 6, 30, 54 },
            new int[]{ 6, 32, 58 },
            new int[]{ 6, 34, 62 },
            new int[]{ 6, 26, 46, 66 },
            new int[]{ 6, 26, 48, 70 },
            new int[]{ 6, 26, 50, 74 },
            new int[]{ 6, 30, 54, 78 },
            new int[]{ 6, 30, 56, 82 },
            new int[]{ 6, 30, 58, 86 },
            new int[]{ 6, 34, 62, 90 },
            new int[]{ 6, 28, 50, 72, 94 },
            new int[]{ 6, 26, 50, 74, 98 },
            new int[]{ 6, 30, 54, 78, 102 },
            new int[]{ 6, 28, 54, 80, 106 },
            new int[]{ 6, 32, 58, 84, 110 },
            new int[]{ 6, 30, 58, 86, 114 },
            new int[]{ 6, 34, 62, 90, 118 },
            new int[]{ 6, 26, 50, 74, 98, 122 },
            new int[]{ 6, 30, 54, 78, 102, 126 },
            new int[]{ 6, 26, 52, 78, 104, 130 },
            new int[]{ 6, 30, 56, 82, 108, 134 },
            new int[]{ 6, 34, 60, 86, 112, 138 },
            new int[]{ 6, 30, 58, 86, 114, 142 },
            new int[]{ 6, 34, 62, 90, 118, 146 },
            new int[]{ 6, 30, 54, 78, 102, 126, 150 },
            new int[]{ 6, 24, 50, 76, 102, 128, 154 },
            new int[]{ 6, 28, 54, 80, 106, 132, 158 },
            new int[]{ 6, 32, 58, 84, 110, 136, 162 },
            new int[]{ 6, 26, 54, 82, 110, 138, 166 },
            new int[]{ 6, 30, 58, 86, 114, 142, 170 }
        };
        /// <summary>
        /// Коды версий
        /// </summary>
        private static readonly Dictionary<int, BitSequence[]> VersionPattern = new Dictionary<int, BitSequence[]>()
        {
           {7, new BitSequence[]{new BitSequence("000010"), new BitSequence("011110"), new BitSequence("100110") } },
           {8, new BitSequence[]{new BitSequence("010001"), new BitSequence("011100"), new BitSequence("111000") } },
           {9, new BitSequence[]{new BitSequence("110111"), new BitSequence("011000"), new BitSequence("000100") } },
           {10, new BitSequence[]{new BitSequence("101001"), new BitSequence("111110"), new BitSequence("000000") } },
           {11, new BitSequence[]{new BitSequence("001111"), new BitSequence("111010"), new BitSequence("111100") } },
           {12, new BitSequence[]{new BitSequence("001101"), new BitSequence("100100"), new BitSequence("011010") } },
           {13, new BitSequence[]{new BitSequence("101011"), new BitSequence("100000"), new BitSequence("100110") } },
           {14, new BitSequence[]{new BitSequence("110101"), new BitSequence("000110"), new BitSequence("100010") } },
           {15, new BitSequence[]{new BitSequence("010011"), new BitSequence("000010"), new BitSequence("011110") } },
           {16, new BitSequence[]{new BitSequence("011100"), new BitSequence("010001"), new BitSequence("011100") } },
           {17, new BitSequence[]{new BitSequence("111010"), new BitSequence("010101"), new BitSequence("100000") } },
           {18, new BitSequence[]{new BitSequence("100100"), new BitSequence("110011"), new BitSequence("100100") } },
           {19, new BitSequence[]{new BitSequence("000010"), new BitSequence("110111"), new BitSequence("011000") } },
           {20, new BitSequence[]{new BitSequence("000000"), new BitSequence("101001"), new BitSequence("111110") } },
           {21, new BitSequence[]{new BitSequence("100110"), new BitSequence("101101"), new BitSequence("000010") } },
           {22, new BitSequence[]{new BitSequence("111000"), new BitSequence("001011"), new BitSequence("000110") } },
           {23, new BitSequence[]{new BitSequence("011110"), new BitSequence("001111"), new BitSequence("111010") } },
           {24, new BitSequence[]{new BitSequence("001101"), new BitSequence("001101"), new BitSequence("100100") } },
           {25, new BitSequence[]{new BitSequence("101011"), new BitSequence("001001"), new BitSequence("011000") } },
           {26, new BitSequence[]{new BitSequence("110101"), new BitSequence("101111"), new BitSequence("011100") } },
           {27, new BitSequence[]{new BitSequence("010011"), new BitSequence("101011"), new BitSequence("100000") } },
           {28, new BitSequence[]{new BitSequence("010001"), new BitSequence("110101"), new BitSequence("000110") } },
           {29, new BitSequence[]{new BitSequence("110111"), new BitSequence("110001"), new BitSequence("111010") } },
           {30, new BitSequence[]{new BitSequence("101001"), new BitSequence("010111"), new BitSequence("111110") } },
           {31, new BitSequence[]{new BitSequence("001111"), new BitSequence("010011"), new BitSequence("000010") } },
           {32, new BitSequence[]{new BitSequence("101000"), new BitSequence("011000"), new BitSequence("101101") } },
           {33, new BitSequence[]{new BitSequence("001110"), new BitSequence("011100"), new BitSequence("010001") } },
           {34, new BitSequence[]{new BitSequence("010000"), new BitSequence("111010"), new BitSequence("010101") } },
           {35, new BitSequence[]{new BitSequence("110110"), new BitSequence("111110"), new BitSequence("101001") } },
           {36, new BitSequence[]{new BitSequence("110100"), new BitSequence("100000"), new BitSequence("001111") } },
           {37, new BitSequence[]{new BitSequence("010010"), new BitSequence("100100"), new BitSequence("110011") } },
           {38, new BitSequence[]{new BitSequence("001100"), new BitSequence("000010"), new BitSequence("110111") } },
           {39, new BitSequence[]{new BitSequence("101010"), new BitSequence("000110"), new BitSequence("001011") } },
           {40, new BitSequence[]{new BitSequence("111001"), new BitSequence("000100"), new BitSequence("010101") } }
        };
        /// <summary>
        /// Коды маски и уровня коррекции
        /// </summary>
        private static readonly BitSequence[,] MaskLevelPattern = new BitSequence[,]
        {
            { new BitSequence("111011111000100"), new BitSequence("101010000010010"), new BitSequence("011010101011111"), new BitSequence("001011010001001") },
            { new BitSequence("111001011110011"), new BitSequence("101000100100101"), new BitSequence("011000001101000"), new BitSequence("001001110111110") },
            { new BitSequence("111110110101010"), new BitSequence("101111001111100"), new BitSequence("011111100110001"), new BitSequence("001110011100111") },
            { new BitSequence("111100010011101"), new BitSequence("101101101001011"), new BitSequence("011101000000110"), new BitSequence("001100111010000") },
            { new BitSequence("110011000101111"), new BitSequence("100010111111001"), new BitSequence("010010010110100"), new BitSequence("000011101100010") },
            { new BitSequence("110001100011000"), new BitSequence("100000011001110"), new BitSequence("010000110000011"), new BitSequence("000001001010101") },
            { new BitSequence("110110001000001"), new BitSequence("100111110010111"), new BitSequence("010111011011010"), new BitSequence("000110100001100") },
            { new BitSequence("110100101110110"), new BitSequence("100101010100000"), new BitSequence("010101111101101"), new BitSequence("000100000111011") }
        };
        /// <summary>
        /// Функция инвертирования данных (в зависимости от маски)
        /// </summary>
        private static readonly Func<int, int, bool>[] MaskFunction = new Func<int, int, bool>[]
        {
            (x, y) => (((x+y)%2)==0),
            (x, y) => ((y%2)==0),
            (x, y) => ((x%3)==0),
            (x, y) => (((x+y)%3)==0),
            (x, y) => (((x/3+y/2)%2)==0),
            (x, y) => (((x*y)%2+(x*y)%3)==0),
            (x, y) => ((((x*y)%2+(x*y)%3)%2)==0),
            (x, y) => ((((x*y)%3+(x+y)%2)%2)==0)
        };

        #endregion

        #region Тексты ошибок
        private const string ErrorBadVersion = "Неверное указание версии";
        private const string ErrorBadLevel = "Неверное указание уровня коррекции";
        private const string ErrorBadMask = "Неверное указание кода маски";
        private const string ErrorZoom = "Недопустимый масштаб";
        private const string ErrorLongData = "Данным не хватает места для указанной версии и/или уровня коррекции";
        private const string ErrorNullData = "Отсутствуют данные";
        #endregion
    }

    /// <summary>
    /// Последовательность бит
    /// </summary>
    public class BitSequence
    {
        /// <summary>
        /// Создание последовательности бит из массива bool
        /// </summary>
        /// <param name="Bits">Массив bool</param>
        public BitSequence(bool[] Bits)
        {
            this.Bits = new BitArray(Bits);
        }
        /// <summary>
        /// Создание последовательности бит из массива байтов
        /// </summary>
        /// <param name="Bytes">Массив байтов</param>
        /// <param name="IsBigEndian">Признак "прямого" следования бит в байте (старший бит впереди)</param>
        public BitSequence(byte[] Bytes, bool IsBigEndian)
        {
            if (IsBigEndian)
            {
                Bits = new BitArray(8 * Bytes.Length);
                for (int i = 0; i < Bytes.Length; i++)
                {
                    Bits[8 * i + 0] = ((Bytes[i] & 0b10000000) != 0);
                    Bits[8 * i + 1] = ((Bytes[i] & 0b01000000) != 0);
                    Bits[8 * i + 2] = ((Bytes[i] & 0b00100000) != 0);
                    Bits[8 * i + 3] = ((Bytes[i] & 0b00010000) != 0);
                    Bits[8 * i + 4] = ((Bytes[i] & 0b00001000) != 0);
                    Bits[8 * i + 5] = ((Bytes[i] & 0b00000100) != 0);
                    Bits[8 * i + 6] = ((Bytes[i] & 0b00000010) != 0);
                    Bits[8 * i + 7] = ((Bytes[i] & 0b00000001) != 0);
                }
            }
            else
                Bits = new BitArray(Bytes);
        }
        /// <summary>
        /// Создание последовательности бит из массива байтов (старший бит впереди)
        /// </summary>
        /// <param name="Bytes">Массив байтов</param>
        public BitSequence(byte[] Bytes) : this(Bytes, true) { }
        /// <summary>
        /// Создание последовательности бит из строки (обрабатываются только '0' и '1')
        /// </summary>
        /// <param name="Str">Строка</param>
        public BitSequence(string Str) : this((from c in Str where ((c=='0')||(c=='1')) select (c=='1')).ToArray()) { }
        /// <summary>
        /// Создание последовательности бит указанной длины
        /// </summary>
        /// <param name="Length">Длина последовательности</param>
        /// <param name="Value">Значения бит</param>
        public BitSequence(int Length, bool Value)
        {
            Bits = new BitArray(Length, Value);
        }
        /// <summary>
        /// Создание последовательности бит (нулевого значения) указанной длины
        /// </summary>
        /// <param name="Length">Длина последовательности</param>
        public BitSequence(int Length) : this(Length, false) { }

        /// <summary>
        /// Обращение к битам
        /// </summary>
        /// <param name="Index">Индекс бита</param>
        /// <returns>Значение бита</returns>
        public bool this[int Index]
        {
            get { return Bits[Index]; }
        }
        /// <summary>
        /// Количество бит в последовательности
        /// </summary>
        public int Length { get { return Bits.Count; } }

        /// <summary>
        /// Получение массива байт (при необходимости дополняется нулевыми битами)
        /// </summary>
        /// <param name="IsBigEndian">Признак "прямого" следования бит в байте (старший бит впереди)</param>
        /// <returns>Массив байт</returns>
        public byte[] ToByteArray(bool IsBigEndian)
        {
            int n = (Length & 0b00000111);
            BitSequence tmp = (n == 0 ? this : this + new BitSequence(8 - n));
            byte[] Result = new byte[tmp.Length / 8];
            for (int i = 0; i < Result.Length; i++)
            {
                byte b = 0b00000000;
                if (IsBigEndian)
                {
                    if (tmp[8 * i + 0]) b |= 0b10000000;
                    if (tmp[8 * i + 1]) b |= 0b01000000;
                    if (tmp[8 * i + 2]) b |= 0b00100000;
                    if (tmp[8 * i + 3]) b |= 0b00010000;
                    if (tmp[8 * i + 4]) b |= 0b00001000;
                    if (tmp[8 * i + 5]) b |= 0b00000100;
                    if (tmp[8 * i + 6]) b |= 0b00000010;
                    if (tmp[8 * i + 7]) b |= 0b00000001;
                }
                else
                {
                    if (tmp[8 * i + 0]) b |= 0b00000001;
                    if (tmp[8 * i + 1]) b |= 0b00000010;
                    if (tmp[8 * i + 2]) b |= 0b00000100;
                    if (tmp[8 * i + 3]) b |= 0b00001000;
                    if (tmp[8 * i + 4]) b |= 0b00010000;
                    if (tmp[8 * i + 5]) b |= 0b00100000;
                    if (tmp[8 * i + 6]) b |= 0b01000000;
                    if (tmp[8 * i + 7]) b |= 0b10000000;
                }
                Result[i] = b;
            }
            return Result;
        }
        /// <summary>
        /// Получение массива байт (при необходимости дополняется нулевыми битами), где старший бит впереди
        /// </summary>
        /// <returns>Массив байт</returns>
        public byte[] ToByteArray() { return ToByteArray(true); }

        /// <summary>
        /// Строковое представление
        /// </summary>
        /// <returns>Строковое представление</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (bool x in Bits)
                sb.Append(x ? '1' : '0');
            return sb.ToString();
        }

        /// <summary>
        /// Сложение последовательностей
        /// </summary>
        /// <param name="a">Левый операнд</param>
        /// <param name="b">Правый операнд</param>
        /// <returns>Результат сложения</returns>
        public static BitSequence operator +(BitSequence a, BitSequence b)
        {
            BitSequence Result = new BitSequence(a.Length + b.Length);
            for (int i = 0; i < a.Length; i++)
                Result.Bits[i] = a[i];
            for (int i = 0; i < b.Length; i++)
                Result.Bits[a.Length + i] = b[i];
            return Result;
        }

        /// <summary>
        /// Массив бит
        /// </summary>
        private readonly BitArray Bits;
    }
}