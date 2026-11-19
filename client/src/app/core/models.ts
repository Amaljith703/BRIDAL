export type OrderItemType = 'Product' | 'Service' | 'Package' | 'Combo';
export type OrderStatus =
  | 'Pending'
  | 'PaymentPending'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Refunded'
  | 'Failed';
export type PaymentStatus = 'Created' | 'Authorized' | 'Captured' | 'Failed' | 'Refunded' | 'PartiallyRefunded';
export type BookingStatus = 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled';
export type ContactStatus = 'New' | 'Read' | 'Replied' | 'Closed';
export type TicketStatus = 'Open' | 'Pending' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent';
export type ReviewStatus = 'Pending' | 'Approved' | 'Rejected';
export type TestimonialStatus = 'Pending' | 'Approved' | 'Rejected';
export type VlogType = 'Makeup' | 'Influencer' | 'Studio';
export type DiscountType = 'Percentage' | 'Fixed';
export type CouponScope = 'All' | 'Products' | 'Services' | 'Packages';
export type DiscountAppliesTo = 'All' | 'Products' | 'Services' | 'Packages' | 'Combos';
export type MediaType = 'Image' | 'Video' | 'Audio' | 'Document' | 'Model3D';
export type RefundStatus = 'Pending' | 'Processed' | 'Failed';

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface SiteSettings {
  id: string;
  businessName: string;
  tagline: string;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  email: string;
  phone: string;
  whatsApp?: string | null;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  mapLatitude: number;
  mapLongitude: number;
  mapZoom: number;
  googleMapsEmbedUrl?: string | null;
  businessHours?: string | null;
  currency: string;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  surfaceColor: string;
  announcementText?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  musicEnabled: boolean;
  defaultMusicTrackId?: string | null;
  musicVolume: number;
  gstEnabled: boolean;
  pricesIncludeTax: boolean;
  gstNumber?: string | null;
  businessState: string;
  instagramUrl?: string | null;
  facebookUrl?: string | null;
  youtubeUrl?: string | null;
  pinterestUrl?: string | null;
  tiktokUrl?: string | null;
  xUrl?: string | null;
  maintenanceMode: boolean;
  supportEmail?: string | null;
  supportPhone?: string | null;
  heroBadge?: string | null;
  footerNote?: string | null;
}

export interface SocialLink {
  id: string;
  platform: string;
  url: string;
  icon: string;
  sortOrder: number;
  isActive: boolean;
}

export interface MusicTrack {
  id: string;
  title: string;
  artist: string;
  url: string;
  durationSeconds: number;
  isDefault: boolean;
  sortOrder: number;
  credit?: string | null;
  isActive: boolean;
}

export interface HeroSlide {
  id: string;
  eyebrow: string;
  title: string;
  subtitle: string;
  modelImageUrl?: string | null;
  backgroundImageUrl?: string | null;
  ctaLabel?: string | null;
  ctaUrl?: string | null;
  secondaryCtaLabel?: string | null;
  secondaryCtaUrl?: string | null;
  accentColor: string;
  secondaryColor: string;
  effectJson?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface PageSummary {
  id: string;
  key: string;
  title: string;
  subtitle?: string | null;
  isPublished: boolean;
  sortOrder: number;
}

export interface ContentSection {
  id: string;
  key: string;
  sectionType: string;
  eyebrow?: string | null;
  heading?: string | null;
  subheading?: string | null;
  bodyHtml?: string | null;
  mediaUrl?: string | null;
  secondaryMediaUrl?: string | null;
  ctaLabel?: string | null;
  ctaUrl?: string | null;
  configJson?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface ContentPage {
  id: string;
  key: string;
  title: string;
  subtitle?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  isPublished: boolean;
  isActive: boolean;
  sections: ContentSection[];
}

export interface Banner {
  id: string;
  title: string;
  subtitle?: string | null;
  imageUrl: string;
  linkUrl?: string | null;
  placement: string;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface Testimonial {
  id: string;
  customerName: string;
  role?: string | null;
  quote: string;
  rating: number;
  imageUrl?: string | null;
  videoUrl?: string | null;
  isFeatured: boolean;
  status: TestimonialStatus;
  sortOrder: number;
  createdAtUtc: string;
  isActive: boolean;
}

export interface Faq {
  id: string;
  question: string;
  answer: string;
  category: string;
  sortOrder: number;
  isActive: boolean;
}

export interface PortfolioItem {
  id: string;
  title: string;
  category: string;
  description?: string | null;
  imageUrl: string;
  videoUrl?: string | null;
  tagsCsv?: string | null;
  isFeatured: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface Vlog {
  id: string;
  title: string;
  slug: string;
  description?: string | null;
  type: VlogType;
  videoUrl?: string | null;
  thumbnailUrl?: string | null;
  durationSeconds: number;
  influencerName?: string | null;
  publishedAtUtc: string;
  isFeatured: boolean;
  viewCount: number;
  sortOrder: number;
  isActive: boolean;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  parentId?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface ServiceCategory {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface ProductImage {
  id: string;
  url: string;
  altText?: string | null;
  sortOrder: number;
}

export interface ProductVariant {
  id: string;
  name: string;
  sku: string;
  priceOverride?: number | null;
  stockQuantity: number;
  imageUrl?: string | null;
  sortOrder: number;
}

export interface Review {
  id: string;
  productId: string;
  productName: string;
  userName: string;
  rating: number;
  title: string;
  body: string;
  isVerifiedPurchase: boolean;
  status: ReviewStatus;
  adminResponse?: string | null;
  createdAtUtc: string;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  sku: string;
  shortDescription?: string | null;
  description?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
  brand?: string | null;
  price: number;
  compareAtPrice?: number | null;
  stockQuantity: number;
  isFeatured: boolean;
  isPromoted: boolean;
  isNewArrival: boolean;
  ratingAverage: number;
  ratingCount: number;
  imageUrl?: string | null;
  tagsCsv?: string | null;
  hsnCode?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface ProductDetail extends Product {
  costPrice?: number | null;
  lowStockThreshold: number;
  seoTitle?: string | null;
  seoDescription?: string | null;
  weightGrams: number;
  taxRateId?: string | null;
  images: ProductImage[];
  variants: ProductVariant[];
  reviews: Review[];
}

export interface Service {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string | null;
  description?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
  price: number;
  compareAtPrice?: number | null;
  durationMinutes: number;
  imageUrl?: string | null;
  isFeatured: boolean;
  hsnCode?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface PackageItem {
  id: string;
  serviceId: string;
  serviceName: string;
  quantity: number;
  sortOrder: number;
}

export interface ServicePackage {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string | null;
  description?: string | null;
  price: number;
  compareAtPrice?: number | null;
  durationMinutes: number;
  imageUrl?: string | null;
  occasion?: string | null;
  isFeatured: boolean;
  hsnCode?: string | null;
  sortOrder: number;
  isActive: boolean;
  items: PackageItem[];
}

export interface ComboItem {
  id: string;
  productId?: string | null;
  productName?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
  packageId?: string | null;
  packageName?: string | null;
  quantity: number;
  sortOrder: number;
}

export interface Combo {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  price: number;
  compareAtPrice?: number | null;
  imageUrl?: string | null;
  isFeatured: boolean;
  sortOrder: number;
  isActive: boolean;
  items: ComboItem[];
}

export interface Offer {
  id: string;
  title: string;
  subtitle?: string | null;
  description?: string | null;
  code?: string | null;
  discountType: DiscountType;
  discountValue: number;
  imageUrl?: string | null;
  appliesTo: DiscountAppliesTo;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  isFeatured: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface Coupon {
  id: string;
  code: string;
  description?: string | null;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount: number;
  maximumDiscountAmount?: number | null;
  scope: CouponScope;
  startsAtUtc: string;
  endsAtUtc: string;
  usageLimit: number;
  perUserLimit: number;
  usedCount: number;
  isActive: boolean;
}

export interface TaxRate {
  id: string;
  name: string;
  ratePercent: number;
  country: string;
  state?: string | null;
  hsnCode?: string | null;
  description?: string | null;
  isDefault: boolean;
  isActive: boolean;
}

export interface ShippingMethod {
  id: string;
  name: string;
  description?: string | null;
  price: number;
  freeAboveAmount?: number | null;
  estimatedDays: string;
  isDefault: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface MediaAsset {
  id: string;
  fileName: string;
  originalFileName: string;
  url: string;
  mediaType: MediaType;
  contentType: string;
  sizeBytes: number;
  width?: number | null;
  height?: number | null;
  durationSeconds?: number | null;
  altText?: string | null;
  credit?: string | null;
  folder: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface Bootstrap {
  settings: SiteSettings;
  socialLinks: SocialLink[];
  music: MusicTrack[];
  categories: Category[];
  heroSlides: HeroSlide[];
  featuredServices: Service[];
  featuredProducts: Product[];
  featuredPackages: ServicePackage[];
  testimonials: Testimonial[];
  faqs: Faq[];
  portfolio: PortfolioItem[];
  vlogs: Vlog[];
  offers: Offer[];
  banners: Banner[];
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  phone?: string | null;
  profileImageUrl?: string | null;
  roles: string[];
  twoFactorEnabled: boolean;
  isActive: boolean;
  createdAtUtc: string;
  marketingOptIn: boolean;
}

export interface AuthResponse {
  accessToken?: string | null;
  expiresAtUtc?: string | null;
  user?: User | null;
  requiresTwoFactor: boolean;
  email?: string | null;
}

export interface Address {
  id: string;
  label: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefaultShipping: boolean;
  isDefaultBilling: boolean;
}

export interface AddressInput {
  label: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefaultShipping: boolean;
  isDefaultBilling: boolean;
}

export interface CartLine {
  key: string;
  itemType: OrderItemType;
  itemId: string;
  name: string;
  slug?: string | null;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  variantName?: string | null;
  maxQuantity: number;
}

export interface PriceBreakdown {
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  shippingAmount: number;
  totalAmount: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  currency: string;
  pricesIncludeTax: boolean;
}

export interface CheckoutQuoteLine {
  itemId: string;
  itemType: OrderItemType;
  name: string;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  taxRatePercent: number;
  taxAmount: number;
  lineTotal: number;
  hsnCode?: string | null;
  variantName?: string | null;
}

export interface CouponResult {
  applied: boolean;
  code?: string | null;
  message?: string | null;
  discountAmount: number;
}

export interface CheckoutQuote {
  lines: CheckoutQuoteLine[];
  breakdown: PriceBreakdown;
  coupon: CouponResult;
  hasPhysicalItems: boolean;
  shippingMethodId?: string | null;
  shippingMethodName: string;
}

export interface CheckoutResponse {
  orderId: string;
  orderNumber: string;
  amount: number;
  currency: string;
  provider: string;
  providerOrderId?: string | null;
  providerKeyId?: string | null;
  requiresPayment: boolean;
  breakdown: PriceBreakdown;
  lines: CheckoutQuoteLine[];
}

export interface OrderItem {
  id: string;
  itemType: OrderItemType;
  itemId: string;
  name: string;
  sku?: string | null;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  taxRatePercent: number;
  taxAmount: number;
  lineTotal: number;
  variantName?: string | null;
}

export interface Payment {
  id: string;
  provider: string;
  providerOrderId?: string | null;
  providerPaymentId?: string | null;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method?: string | null;
  paidAtUtc?: string | null;
  createdAtUtc: string;
}

export interface OrderStatusEvent {
  status: OrderStatus;
  note?: string | null;
  changedAtUtc: string;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
  currency: string;
  itemCount: number;
  createdAtUtc: string;
  paidAtUtc?: string | null;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  subTotal: number;
  discountAmount: number;
  shippingAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  couponCode?: string | null;
  notes?: string | null;
  shippingMethodName: string;
  trackingNumber?: string | null;
  gstNumber?: string | null;
  placeOfSupply?: string | null;
  isGstInvoice: boolean;
  placedAtUtc?: string | null;
  paidAtUtc?: string | null;
  createdAtUtc: string;
  shippingAddressJson: string;
  billingAddressJson: string;
  items: OrderItem[];
  payments: Payment[];
  invoiceNumber?: string | null;
  statusHistory: OrderStatusEvent[];
}

export interface Booking {
  id: string;
  bookingNumber: string;
  fullName: string;
  email: string;
  phone: string;
  serviceName?: string | null;
  packageName?: string | null;
  preferredDate: string;
  preferredTime: string;
  occasion?: string | null;
  notes?: string | null;
  status: BookingStatus;
  adminNotes?: string | null;
  createdAtUtc: string;
}

export interface SupportMessage {
  id: string;
  senderName: string;
  senderType: string;
  body: string;
  attachmentUrl?: string | null;
  isInternal: boolean;
  createdAtUtc: string;
}

export interface SupportTicketSummary {
  id: string;
  ticketNumber: string;
  subject: string;
  category: string;
  priority: TicketPriority;
  status: TicketStatus;
  messageCount: number;
  lastMessageAtUtc: string;
  createdAtUtc: string;
}

export interface SupportTicket {
  id: string;
  ticketNumber: string;
  userId: string;
  customerName?: string | null;
  customerEmail?: string | null;
  subject: string;
  category: string;
  priority: TicketPriority;
  status: TicketStatus;
  lastMessageAtUtc: string;
  createdAtUtc: string;
  messages: SupportMessage[];
}

export interface ContactMessage {
  id: string;
  name: string;
  email: string;
  phone?: string | null;
  subject: string;
  message: string;
  status: ContactStatus;
  adminNotes?: string | null;
  createdAtUtc: string;
}

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  phone?: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  createdAtUtc: string;
  lastLoginAtUtc?: string | null;
  roles: string[];
  orderCount: number;
  totalSpend: number;
}

export interface AdminReviewItem {
  id: string;
  productId: string;
  productName: string;
  userName?: string | null;
  userEmail?: string | null;
  rating: number;
  title: string;
  body: string;
  status: ReviewStatus;
  isVerifiedPurchase: boolean;
  adminResponse?: string | null;
  createdAtUtc: string;
}

export interface AdminRefund {
  id: string;
  amount: number;
  reason: string;
  status: RefundStatus;
  providerRefundId?: string | null;
  processedAtUtc?: string | null;
  createdAtUtc: string;
}

export interface AdminOrder extends Omit<Order, 'payments'> {
  adminNotes?: string | null;
  billingAddressJson: string;
  payments: Payment[];
  refunds: AdminRefund[];
}

export interface AdminCustomerDetail {
  user: AdminUser;
  adminNotes?: string | null;
  addresses: Address[];
  recentOrders: OrderSummary[];
}

export interface AuditLog {
  id: string;
  action: string;
  entityName: string;
  entityId?: string | null;
  details?: string | null;
  userEmail?: string | null;
  ipAddress?: string | null;
  createdAtUtc: string;
}

export interface BackupRecord {
  id: string;
  fileName: string;
  sizeBytes: number;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  status: 'Started' | 'Completed' | 'Failed';
  message?: string | null;
  backupType: string;
}

export interface ChartPoint {
  label: string;
  value: number;
}

export interface TopItem {
  id?: string | null;
  name: string;
  quantity: number;
  revenue: number;
  imageUrl?: string | null;
}

export interface StatusSlice {
  status: string;
  count: number;
  value: number;
}

export interface RecentOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  totalAmount: number;
  status: string;
  createdAtUtc: string;
}

export interface Dashboard {
  revenueToday: number;
  revenue7d: number;
  revenue30d: number;
  ordersToday: number;
  orders30d: number;
  averageOrderValue: number;
  totalCustomers: number;
  newCustomers30d: number;
  pageViews30d: number;
  pendingOrders: number;
  lowStockCount: number;
  pendingReviews: number;
  pendingBookings: number;
  openTickets: number;
  revenueSeries: ChartPoint[];
  orderSeries: ChartPoint[];
  topProducts: TopItem[];
  topServices: TopItem[];
  orderStatuses: StatusSlice[];
  paymentMethods: StatusSlice[];
  recentOrders: RecentOrder[];
  aiInsight?: string | null;
}

export interface AnalyticsDashboard {
  pageViews: number;
  uniqueSessions: number;
  productViews: number;
  addToCarts: number;
  checkouts: number;
  conversionRate: number;
  pageViewSeries: ChartPoint[];
  topPages: ChartPoint[];
  trafficSources: ChartPoint[];
  deviceSplit: ChartPoint[];
  eventBreakdown: ChartPoint[];
}

export interface PaymentConfig {
  defaultProvider: string;
  providers: string[];
  razorpayKeyId?: string | null;
  razorpayEnabled: boolean;
  demoMode: boolean;
  currency: string;
  methods: string[];
}

export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AiChatResponse {
  content: string;
  usedFallback: boolean;
  model: string;
  recommendedProductIds: string[];
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  isActive?: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
