using Infrastructure;
using Infrastructure.Attribute;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JySupplierService : BaseService<JySupplier>
    {
        public PagedInfo<JySupplierDto> List(JyActor actor, JySupplierQueryDto query)
        {
            query ??= new JySupplierQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(!string.IsNullOrEmpty(keyword), it =>
                    it.SupplierCode.Contains(keyword) || it.SupplierName.Contains(keyword)
                    || it.ContactName.Contains(keyword) || it.ContactPhone.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.SupplierCode).ToPageList(page, size, ref total);
            return new PagedInfo<JySupplierDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(ToDto).ToList()
            };
        }

        public long Save(JyActor actor, JySupplierEditDto dto)
        {
            dto.SupplierCode = JyBillNo.NormalizeCode(dto.SupplierCode, "供应商编码");
            dto.SupplierName = JyBillNo.NormalizeName(dto.SupplierName, "供应商名称", 100);
            dto.ContactName = EmptyOrMax(dto.ContactName, 50, "联系人");
            dto.ContactPhone = NormalizePhone(dto.ContactPhone);
            dto.Address = EmptyOrMax(dto.Address, 200, "地址");
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.Status != 0 && dto.Status != 1)
            {
                throw new CustomException("状态不正确");
            }
            EnsureCodeFree(actor.TenantId, dto.SupplierCode, dto.Id);

            if (dto.Id <= 0)
            {
                return Context.Insertable(new JySupplier
                {
                    TenantId = actor.TenantId,
                    StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                    SupplierCode = dto.SupplierCode,
                    SupplierName = dto.SupplierName,
                    ContactName = dto.ContactName,
                    ContactPhone = dto.ContactPhone,
                    Address = dto.Address,
                    Status = dto.Status,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
            }

            var current = Must(actor.TenantId, dto.Id);
            current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
            current.SupplierCode = dto.SupplierCode;
            current.SupplierName = dto.SupplierName;
            current.ContactName = dto.ContactName;
            current.ContactPhone = dto.ContactPhone;
            current.Address = dto.Address;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.StoreId,
                    it.SupplierCode,
                    it.SupplierName,
                    it.ContactName,
                    it.ContactPhone,
                    it.Address,
                    it.Status,
                    it.Remark,
                    it.Update_by,
                    it.Update_time
                })
                .ExecuteCommand();
            return current.Id;
        }

        public int Delete(JyActor actor, long id)
        {
            Must(actor.TenantId, id);
            return Context.Updateable<JySupplier>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        private void EnsureCodeFree(string tenantId, string code, long exceptId)
        {
            var used = Context.Queryable<JySupplier>().Any(it =>
                it.TenantId == tenantId && it.SupplierCode == code && it.DelFlag == 0 && it.Id != exceptId);
            if (used)
            {
                throw new CustomException("供应商编码已存在");
            }
        }

        private JySupplier Must(string tenantId, long id)
        {
            return Context.Queryable<JySupplier>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("供应商不存在");
        }

        private static JySupplierDto ToDto(JySupplier it)
        {
            return new JySupplierDto
            {
                Id = it.Id,
                StoreId = it.StoreId,
                SupplierCode = it.SupplierCode,
                SupplierName = it.SupplierName,
                ContactName = it.ContactName,
                ContactPhone = it.ContactPhone,
                Address = it.Address,
                Status = it.Status,
                Remark = it.Remark,
                CreateTime = it.Create_time
            };
        }

        private static string NormalizePhone(string phone)
        {
            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                return null;
            }
            if (phone.Length > 30 || phone.Any(ch => !char.IsDigit(ch) && ch != '-' && ch != '+'))
            {
                throw new CustomException("联系电话只能包含数字、加号或中横线，最长 30 位");
            }
            return phone;
        }

        private static string EmptyOrMax(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (value.Length > max)
            {
                throw new CustomException($"{label}不能超过 {max} 个字");
            }
            return value;
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark))
            {
                return null;
            }
            if (remark.Length > 500)
            {
                throw new CustomException("备注不能超过 500 个字");
            }
            return remark;
        }
    }
}
