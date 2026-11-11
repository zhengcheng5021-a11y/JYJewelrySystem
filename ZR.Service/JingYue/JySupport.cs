using System.Text.RegularExpressions;
using Infrastructure;
using ZR.Model.JingYue;

namespace ZR.Service.JingYue
{
    public sealed class JyActor
    {
        public string TenantId { get; }
        public string UserName { get; }
        public long? UserId { get; }

        public JyActor(string tenantId, string userName, long? userId = null)
        {
            // 非多租户部署统一落到主库配置，避免前端历史 tenant0 与主库 0 不一致导致业务数据被过滤。
            TenantId = !App.IsTenantEnabled()
                ? App.MainDbConfigId
                : (string.IsNullOrWhiteSpace(tenantId) ? App.MainDbConfigId : tenantId.Trim());
            UserName = string.IsNullOrWhiteSpace(userName) ? "system" : userName.Trim();
            UserId = userId > 0 ? userId : null;
            if (TenantId.Length > 64)
            {
                throw new CustomException("租户标识过长");
            }
        }
    }

    public static class JyBillNo
    {
        public static readonly string[] DatePatterns = ["", "yyyy", "yyyyMM", "yyyyMMdd", "yyMMdd"];
        public static readonly string[] ResetCycles = ["none", "year", "month", "day"];
        private static readonly Regex PrefixPattern = new("^[A-Z0-9]{0,8}$", RegexOptions.Compiled);
        private static readonly Regex CodePattern = new("^[A-Za-z][A-Za-z0-9_-]{0,31}$", RegexOptions.Compiled);

        public static string NormalizePrefix(string prefix)
        {
            prefix = (prefix ?? string.Empty).Trim().ToUpperInvariant();
            if (!PrefixPattern.IsMatch(prefix))
            {
                throw new CustomException("前缀只能包含字母和数字，可以为空，长度 0 到 8 位");
            }
            return prefix;
        }

        public static string NormalizeCode(string code, string label)
        {
            code = (code ?? string.Empty).Trim();
            if (!CodePattern.IsMatch(code))
            {
                throw new CustomException($"{label}需以字母开头，只能包含字母、数字、下划线或中横线，最长 32 位");
            }
            return code;
        }

        public static string NormalizeName(string name, string label, int max)
        {
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name) || name.Length > max)
            {
                throw new CustomException($"{label}不能为空，且不能超过 {max} 个字");
            }
            return name;
        }

        public static string NormalizePattern(string pattern)
        {
            pattern ??= string.Empty;
            if (!DatePatterns.Contains(pattern))
            {
                throw new CustomException("日期格式只允许：不带日期、yyyy、yyyyMM、yyyyMMdd、yyMMdd");
            }
            return pattern;
        }

        public static string NormalizeCycle(string cycle)
        {
            cycle = (cycle ?? string.Empty).Trim().ToLowerInvariant();
            if (!ResetCycles.Contains(cycle))
            {
                throw new CustomException("重置周期只允许：不重置、按年、按月、按日");
            }
            return cycle;
        }

        public static void ValidateRule(string prefix, string datePattern, int seqLength, string resetCycle)
        {
            if (seqLength < 2 || seqLength > 8)
            {
                throw new CustomException("流水号长度必须在 2 到 8 位之间");
            }
            var rank = resetCycle switch
            {
                "day" => 3,
                "month" => 2,
                "year" => 1,
                _ => 0
            };
            var patternRank = datePattern switch
            {
                "yyyyMMdd" or "yyMMdd" => 3,
                "yyyyMM" => 2,
                "yyyy" => 1,
                _ => 0
            };
            if (rank > patternRank)
            {
                throw new CustomException("重置周期不能比日期格式更细，否则不同周期会生成同一个单号");
            }
            var sample = Compose(prefix, datePattern, seqLength, DateTime.Now, 1);
            if (sample.Length > 32)
            {
                throw new CustomException("按当前规则生成的单号超过 32 位，请缩短前缀、日期或流水长度");
            }
        }

        public static string CycleKey(string resetCycle, DateTime now)
        {
            return resetCycle switch
            {
                "day" => now.ToString("yyyyMMdd"),
                "month" => now.ToString("yyyyMM"),
                "year" => now.ToString("yyyy"),
                _ => "ALL"
            };
        }

        public static string Compose(string prefix, string datePattern, int seqLength, DateTime now, int seq)
        {
            var date = string.IsNullOrEmpty(datePattern) ? string.Empty : now.ToString(datePattern);
            return prefix + date + seq.ToString().PadLeft(seqLength, '0');
        }

        public static string NextPreview(JyBillRule rule, DateTime now)
        {
            var key = CycleKey(rule.ResetCycle, now);
            var seq = rule.CurrentKey == key ? rule.CurrentSeq + 1 : 1;
            return Compose(rule.Prefix, rule.DatePattern, rule.SeqLength, now, seq);
        }
    }
}


