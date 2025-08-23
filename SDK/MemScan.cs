using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESDK
{
    public enum ScanOptions
    {
        soUnknownValue = 0,
        soExactValue = 1,
        soValueBetween = 2,
        soBiggerThan = 3,
        soSmallerThan = 4,
        soIncreasedValue = 5,
        soIncreasedValueBy = 6,
        soDecreasedValue = 7,
        soDecreasedValueBy = 8,
        soChanged = 9,
        soUnchanged = 10
    }

    public enum VarTypes
    {
        vtByte = 0,
        vtWord = 1,
        vtDword = 2,
        vtQword = 3,
        vtSingle = 4,
        vtDouble = 5,
        vtString = 6,
        vtUnicodeString = 7,
        vtWideString = 7,
        vtByteArray = 8,
        vtBinary = 9,
        vtAll = 10,
        vtAutoAssembler = 11,
        vtPointer = 12,
        vtCustom = 13,
        vtGrouped = 14
    }

    public enum RoundingTypes
    {
        rtRounded = 0,
        rtExtremerounded = 1,
        rtTruncated = 2
    }

    public enum FastScanMethods
    {
        fsmNotAligned = 0,
        fsmAligned = 1,
        fsmLastDigits = 2
    }

    public class ScanParameters
    {
        public string Value;
        public string Value2;
        public ScanOptions ScanOption;
        public VarTypes VarType;
        public RoundingTypes RoundingType;
        public UInt64 StartAddress;
        public UInt64 StopAddress;
        public string ProtectionFlags;
        public FastScanMethods AlignmentType;
        public string AlignmentValue;
        public bool isHexadecimalInput;
        public bool isUTF16Scan;
        public bool isCaseSensitive;
        public bool isPercentageScan;

        public ScanParameters()
        {
            ScanOption = ScanOptions.soExactValue;
            VarType = VarTypes.vtDword;
            RoundingType = RoundingTypes.rtExtremerounded;
            StartAddress = 0;
            StopAddress = (UInt64)(Int64.MaxValue);
            ProtectionFlags = "+W-C";
            AlignmentType = FastScanMethods.fsmAligned;
            AlignmentValue = "4";
            isHexadecimalInput = false;
            isUTF16Scan = false;
            isCaseSensitive = false;
            isPercentageScan = false;
        }
    }

    public class MemScan : CEObjectWrapper
    {
        private bool scanStarted;

        public delegate void dScanDone(object sender);
        public delegate void dGUIUpdate(object sender, UInt64 TotalAddressesToScan, UInt64 CurrentlyScanned, UInt64 ResultsFound);

        public dScanDone OnScanDone;
        public dGUIUpdate OnGuiUpdate;

        public void WaitTillDone()
        {
            lua.PushCEObject(CEObject);
            lua.PushString("waitTillDone");
            lua.GetTable(-2);

            if (!lua.IsFunction(-1)) throw new System.ApplicationException("memscan object without a waitTillDone method");

            lua.PCall(0, 0);
            lua.SetTop(0);
        }

        public void Scan(ScanParameters p)
        {
            try
            {
                lua.PushCEObject(CEObject);

                if (!scanStarted)
                {
                    lua.PushString("firstScan");
                    lua.GetTable(-2);
                    if (!lua.IsFunction(-1)) throw new System.ApplicationException("memscan object without a firstScan method");

                    lua.PushInteger((long)p.ScanOption);
                    lua.PushInteger((long)p.VarType);
                    lua.PushInteger((long)p.RoundingType);
                    lua.PushString(p.Value);
                    lua.PushString(p.Value2);
                    lua.PushInteger((long)p.StartAddress);
                    lua.PushInteger((long)p.StopAddress);
                    lua.PushString(p.ProtectionFlags);
                    lua.PushInteger((long)p.AlignmentType);
                    lua.PushString(p.AlignmentValue);
                    lua.PushBoolean(p.isHexadecimalInput);
                    lua.PushBoolean(true);
                    lua.PushBoolean(p.isUTF16Scan);
                    lua.PushBoolean(p.isCaseSensitive);
                    lua.PCall(14, 0);

                    scanStarted = true;
                }
                else
                {
                    lua.PushString("nextScan");
                    lua.GetTable(-2);
                    if (!lua.IsFunction(-1)) throw new System.ApplicationException("memscan object without a nextScan method");

                    lua.PushInteger((long)p.ScanOption);
                    lua.PushInteger((long)p.RoundingType);
                    lua.PushString(p.Value);
                    lua.PushString(p.Value2);
                    lua.PushBoolean(p.isHexadecimalInput);
                    lua.PushBoolean(true);
                    lua.PushBoolean(p.isUTF16Scan);
                    lua.PushBoolean(p.isCaseSensitive);
                    lua.PushBoolean(p.isPercentageScan);
                    lua.PCall(9, 0);
                }
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        public void Reset()
        {
            lua.PushCEObject(CEObject);
            lua.PushString("newScan");
            lua.GetTable(-2);

            if (!lua.IsFunction(-1)) throw new System.ApplicationException("memscan object without a newScan method");

            lua.PCall(0, 0);
            lua.SetTop(0);

            scanStarted = false;
        }

        private int LScanDone(IntPtr L)
        {
            OnScanDone?.Invoke(this);
            return 0;
        }

        private int LGuiUpdate(IntPtr L)
        {
            if (OnGuiUpdate != null && lua.GetTop() >= 4)
                OnGuiUpdate(this, (UInt64)lua.ToInteger(2), (UInt64)lua.ToInteger(3), (UInt64)lua.ToInteger(4));
            return 0;
        }

        CESDKLua.LuaCall dLScanDone;
        CESDKLua.LuaCall dLGuiUpdate;

        public MemScan()
        {
            try
            {
                lua.GetGlobal("createMemScan");
                if (lua.IsNil(-1)) throw new System.ApplicationException("You have no createFoundList (WTF)");

                lua.PCall(0, 1);

                if (lua.IsCEObject(-1))
                {
                    CEObject = lua.ToCEObject(-1);

                    dLScanDone = LScanDone;
                    dLGuiUpdate = LGuiUpdate;

                    lua.PushString("OnScanDone");
                    lua.PushFunction(dLScanDone);
                    lua.SetTable(-3);

                    lua.PushString("OnGuiUpdate");
                    lua.PushFunction(dLGuiUpdate);
                    lua.SetTable(-3);
                }
                else
                    throw new System.ApplicationException("No idea what it returned");
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        /// <summary>
        /// Returns a FoundList object attached to this MemScan, or null if none.
        /// </summary>
        public FoundList AttachedFoundList
        {
            get
            {
                lua.PushCEObject(CEObject);               // push the MemScan object
                lua.PushString("getAttachedFoundlist");   // push the method name
                lua.GetTable(-2);                         // get the function
                lua.PCall(0, 1);                          // call the function

                if (lua.IsNil(-1))                        // if result is nil, return null
                {
                    lua.SetTop(0);
                    return null;
                }

                IntPtr foundListPtr = lua.ToCEObject(-1); // get raw CE object pointer
                lua.SetTop(0);

                return new FoundList(foundListPtr);       // wrap it in your FoundList class
            }
        }

    }
}
