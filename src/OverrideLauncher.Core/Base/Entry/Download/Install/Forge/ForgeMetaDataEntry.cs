using System.Xml.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Forge;

public class ForgeMetaDataEntry
{
    [XmlRoot(ElementName = "versioning")]
    public class Versioning
    {
        [XmlElement(ElementName = "latest")]
        public string Latest { get; set; }

        [XmlElement(ElementName = "release")]
        public string Release { get; set; }

        [XmlElement(ElementName = "versions")]
        public Versions Versions { get; set; }

        [XmlElement(ElementName = "lastUpdated")]
        public string LastUpdated { get; set; }
    }

    [XmlRoot(ElementName = "versions")]
    public class Versions
    {
        [XmlElement(ElementName = "version")]
        public List<string> Version { get; set; }
    }

    [XmlRoot(ElementName = "metadata")]
    public class ForgeMetaDataRoot
    {
        [XmlElement(ElementName = "groupId")]
        public string GroupId { get; set; }

        [XmlElement(ElementName = "artifactId")]
        public string ArtifactId { get; set; }

        [XmlElement(ElementName = "versioning")]
        public Versioning Versioning { get; set; }
    }
}