using System;

namespace kibotu
{
    [Serializable]
    public class Asset
    {
        public string imageFullUrl;
        public KibotuMetadata kibotuMetadata;

        public bool IsControlGroup
        {
            get => (kibotuMetadata.variationId == 0);
        }
    }

    [Serializable]
    public class KibotuMetadata
    {
        public string userId;
        public string offerId;
        public string countryIso2;
        public string experimentId;
        public int variationId;
        public string renderId;
        public string occasion;
    }

}