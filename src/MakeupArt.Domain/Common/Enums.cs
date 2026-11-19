namespace MakeupArt.Domain.Common;

public enum MediaType { Image = 1, Video = 2, Audio = 3, Document = 4, Model3D = 5 }
public enum TestimonialStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum VlogType { Makeup = 1, Influencer = 2, Studio = 3 }
public enum DiscountType { Percentage = 1, Fixed = 2 }
public enum CouponScope { All = 0, Products = 1, Services = 2, Packages = 3 }
public enum OrderStatus { Pending = 0, PaymentPending = 1, Paid = 2, Processing = 3, Shipped = 4, Delivered = 5, Cancelled = 6, Refunded = 7, Failed = 8 }
public enum PaymentStatus { Created = 0, Authorized = 1, Captured = 2, Failed = 3, Refunded = 4, PartiallyRefunded = 5 }
public enum PaymentProviderType { Mock = 0, Razorpay = 1, Stripe = 2, PayPal = 3 }
public enum RefundStatus { Pending = 0, Processed = 1, Failed = 2 }
public enum BookingStatus { Pending = 0, Confirmed = 1, Completed = 2, Cancelled = 3 }
public enum ContactStatus { New = 0, Read = 1, Replied = 2, Closed = 3 }
public enum TicketStatus { Open = 0, Pending = 1, Resolved = 2, Closed = 3 }
public enum TicketPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }
public enum SenderType { Customer = 1, Support = 2, System = 3 }
public enum ReviewStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum OrderItemType { Product = 1, Service = 2, Package = 3, Combo = 4 }
public enum BackupStatus { Started = 0, Completed = 1, Failed = 2 }
public enum DiscountAppliesTo { All = 0, Products = 1, Services = 2, Packages = 3, Combos = 4 }
