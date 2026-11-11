namespace ZR.Model.JingYue.Dto
{
    public class JyLookupTypeDto
    {
        public long Id { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public int LevelLimit { get; set; }
        public int IsPreset { get; set; }
        public int Status { get; set; }
        /// <summary>话术星级，1-5 星，默认 3 星。</summary>
        public int SortNo { get; set; }
        public string Remark { get; set; }
        public int ItemCount { get; set; }
    }

    public class JyLookupTypeEditDto
    {
        public long Id { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public int LevelLimit { get; set; } = 1;
        public int Status { get; set; }
        /// <summary>话术星级，1-5 星，默认 3 星。</summary>
        public int SortNo { get; set; }
        public string Remark { get; set; }
    }

    public class JyLookupItemDto
    {
        public long Id { get; set; }
        public long TypeId { get; set; }
        public long? ParentId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int IsPreset { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        public string Remark { get; set; }
        public string ParentName { get; set; }
        public List<JyLookupItemDto> Children { get; set; }
    }

    public class JyLookupItemEditDto
    {
        public long Id { get; set; }
        public long TypeId { get; set; }
        public long? ParentId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        public string Remark { get; set; }
    }

    public class JyLookupOptionDto
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int Level { get; set; }
    }

    public class JySupplierQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
    }

    public class JySupplierDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JySupplierEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyScriptQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? StyleItemId { get; set; }
          public string ScriptType { get; set; }
    }

    public class JyScriptDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string Title { get; set; }
          public string ScriptType { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public long? MetalItemId { get; set; }
        public string StyleName { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public string Content { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyScriptEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string Title { get; set; }
          public string ScriptType { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public long? MetalItemId { get; set; }
        public string Content { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        public string Remark { get; set; }
    }

    public class JyBillRuleDto
    {
        public long Id { get; set; }
        public string BillType { get; set; }
        public string BillName { get; set; }
        public string Prefix { get; set; }
        public string DatePattern { get; set; }
        public int SeqLength { get; set; }
        public string ResetCycle { get; set; }
        public string CurrentKey { get; set; }
        public int CurrentSeq { get; set; }
        public int IsPreset { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public string NextNo { get; set; }
    }

    public class JyBillRuleEditDto
    {
        public long Id { get; set; }
        public string BillType { get; set; }
        public string BillName { get; set; }
        public string Prefix { get; set; }
        public string DatePattern { get; set; }
        public int SeqLength { get; set; } = 4;
        public string ResetCycle { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyBillIssueQueryDto : PagerInfo
    {
        public string BillType { get; set; }
        public string BillNo { get; set; }
    }

    public class JyBillIssueDto
    {
        public long Id { get; set; }
        public string BillType { get; set; }
        public string BillName { get; set; }
        public string BillNo { get; set; }
        public string CycleKey { get; set; }
        public int SeqNo { get; set; }
        public string BizTable { get; set; }
        public long? BizId { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyBillIssueRequestDto
    {
        public string BillType { get; set; }
        public string BizTable { get; set; }
        public long? BizId { get; set; }
    }
}
