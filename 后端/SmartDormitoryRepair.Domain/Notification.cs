namespace SmartDormitoryRepair.Domain
{
    /// <summary>
    /// 通知消息实体
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 接收用户名
        /// </summary>
        public string ReceiverUsername { get; set; } = null!;

        /// <summary>
        /// 消息标题
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// 消息类型（OrderAssigned-工单指派、OrderStatusChanged-状态变更等）
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// 关联的工单ID（可选）
        /// </summary>
        public int? RelatedOrderId { get; set; }

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 已读时间
        /// </summary>
        public DateTime? ReadTime { get; set; }
    }
}
