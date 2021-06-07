using System;
using UnityEngine;

namespace FMODUnity
{
    [Serializable]
    public struct EventReference
    {
        public FMOD.GUID Guid;

#if UNITY_EDITOR
        public string Path;

        public override string ToString()
        {
            return string.Format("{0} ({1})", Guid, Path);
        }

        public bool IsNull
        {
            get
            {
                return string.IsNullOrEmpty(Path) && Guid.IsNull;
            }
        }
#else
        public override string ToString()
        {
            return Guid.ToString();
        }

        public bool IsNull
        {
            get
            {
                return Guid.IsNull;
            }
        }
#endif
    }
}
