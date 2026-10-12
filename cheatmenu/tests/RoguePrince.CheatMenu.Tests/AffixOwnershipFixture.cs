// Test-only IL2CPP boundary models. Never included in the plugin.
public sealed class ItemInstanceData
{
    private static int sequence=500;
    public IntPtr Pointer;
    public ItemData source;
    public int tier;
    public AffixItemFeature.AffixComponentData Affix;
    public AffixHolderItemFeature.AffixHolderComponentData Holder;
    public ItemInstanceData(){}
    public ItemInstanceData(ItemData source,int tier){Pointer=new(++sequence);this.source=source;this.tier=tier;Holder=source.HasComponent<WeaponFeature>()?new():null;}
    public T GetComponent<T>() where T : class => (typeof(T)==typeof(AffixItemFeature.AffixComponentData)?(object)Affix:Holder) as T;
}
public sealed class AffixItemFeature
{
    public sealed class AffixComponentData { public ItemInstanceData targetItem; }
}
