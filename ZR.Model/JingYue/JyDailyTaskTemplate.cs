using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 每日必做模板。启用后每天按服务器日期自动生成当天的必做快照；删除模板只停止以后生成，历史快照保留。
    /// </summary>
    [SugarTable("jy_daily_task_template", "静悦每日必做模板")]
    public class JyDailyTaskTemplate : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(Length = 200)]
        public string Title { get; set; }
        /// <summary>数量或文本，自由填写，例如“5 件”“3 位客户”。</summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Content { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        /// <summary>0 启用，1 停用。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        /// <summary>0 正常，2 已删除。只做逻辑删除，历史当天快照不受影响。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
        /// <summary>模板生效日期，默认创建当天，避免给历史日期补生成快照。</summary>
        public DateTime EffectiveDate { get; set; }
    }
}
