namespace Wms.Domain.Entity
{
    public interface IVersionedEntity
    {
        long Version { get; set; }
    }
}
