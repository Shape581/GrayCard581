using ModKit.ORM;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreyCard581
{
    public class GreyCard_Biz : ModEntity<GreyCard_Biz>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int BizId { get; set; }
        public int CharacterIdOfAdder { get; set; }

        public static async Task<bool> Add(int bizId)
        {
            var query = await Query(obj => obj.BizId == bizId);
            if (!query.Any())
            {
                var instance = new GreyCard_Biz();
                instance.BizId = bizId;
                return await instance.Save();
            }
            return false;
        }
    }
}
