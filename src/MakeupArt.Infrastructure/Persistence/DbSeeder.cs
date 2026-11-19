using System.Text.Json;
using MakeupArt.Application.Pricing;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Content;
using MakeupArt.Domain.Identity;
using MakeupArt.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MakeupArt.Infrastructure.Persistence;

public class DbSeeder(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    IOptions<SeedOptions> seedOptions,
    ILogger<DbSeeder> logger)
{
    private readonly SeedOptions _seed = seedOptions.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync(cancellationToken);
        await EnsureAdminAsync(cancellationToken);
        var customer = await EnsureCustomerAsync(cancellationToken);
        await EnsureSettingsAsync(cancellationToken);
        await EnsureContentAsync(cancellationToken);
        await EnsureCatalogAsync(cancellationToken);
        if (_seed.DemoOrders) await EnsureDemoOrdersAsync(customer, cancellationToken);
        await EnsureExtrasAsync(customer, cancellationToken);
        logger.LogInformation("Database seed completed.");
    }

    private async Task EnsureRolesAsync(CancellationToken ct)
    {
        foreach (var role in new[] { "Admin", "Manager", "Support", "Customer" })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new ApplicationRole { Name = role, Description = $"{role} role" });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureAdminAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_seed.AdminPassword))
        {
            logger.LogWarning("Seed admin password is empty; admin account was not created.");
            return;
        }
        var email = _seed.AdminEmail.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Aarohi",
                LastName = "Sen",
                DisplayName = "Aarohi Sen",
                PhoneNumber = "+91 98765 43210",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            var result = await users.CreateAsync(user, _seed.AdminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create seed admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }
        if (!await users.IsInRoleAsync(user, "Admin")) await users.AddToRoleAsync(user, "Admin");
        if (await db.SaveChangesAsync(ct) > 0) logger.LogInformation("Seed admin ready: {Email}", email);
    }

    private async Task<ApplicationUser> EnsureCustomerAsync(CancellationToken ct)
    {
        var email = _seed.CustomerEmail.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email);
        if (user is not null) return user;
        var password = string.IsNullOrWhiteSpace(_seed.CustomerPassword) ? "MakeupArt@Customer123!" : _seed.CustomerPassword;
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Priya",
            LastName = "Rao",
            DisplayName = "Priya Rao",
            PhoneNumber = "+91 90000 12345",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-60)
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Could not create demo customer: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return user;
        }
        await users.AddToRoleAsync(user, "Customer");
        await db.SaveChangesAsync(ct);
        return user;
    }

    private async Task EnsureSettingsAsync(CancellationToken ct)
    {
        if (await db.SiteSettings.AnyAsync(ct)) return;
        var setting = new SiteSetting
        {
            BusinessName = "Makeup Art",
            Tagline = "Artistry that still looks like you.",
            LogoUrl = "/media/seed/logo.svg",
            FaviconUrl = "/media/seed/favicon.svg",
            Email = "hello@makeupart.local",
            Phone = "+91 98765 43210",
            WhatsApp = "+919876543210",
            AddressLine1 = "12 Rose Atelier, 100 Feet Road",
            AddressLine2 = "Indiranagar",
            City = "Bengaluru",
            State = "Karnataka",
            PostalCode = "560038",
            Country = "India",
            MapLatitude = 12.9716,
            MapLongitude = 77.5946,
            MapZoom = 14,
            BusinessHours = "Mon–Sat · 9:00 AM – 8:00 PM · Sunday by appointment",
            PrimaryColor = "#8E4A5B",
            SecondaryColor = "#C9A26B",
            AccentColor = "#E9C8C1",
            SurfaceColor = "#FAF6F1",
            AnnouncementText = "Complimentary skin consult with every bridal booking.",
            SeoTitle = "Makeup Art | Bridal, Editorial & Everyday Glam",
            SeoDescription = "A boutique makeup studio in Bengaluru for bridal artistry, editorial looks, lessons and curated beauty essentials.",
            MusicEnabled = true,
            MusicVolume = 32,
            GstEnabled = true,
            PricesIncludeTax = true,
            GstNumber = "29ABCDE1234F1Z5",
            BusinessState = "Karnataka",
            SupportEmail = "care@makeupart.local",
            SupportPhone = "+91 98765 43211",
            FooterNote = "Made with care, colour and a little bit of magic."
        };
        db.SiteSettings.Add(setting);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureContentAsync(CancellationToken ct)
    {
        if (await db.Pages.AnyAsync(ct)) return;

        var pages = new List<Page>
        {
            CreatePage("home", "The art of feeling like yourself", "A boutique studio for bridal, editorial and everyday transformation.",
                "Makeup Art | Bridal & Editorial Makeup Studio", "Bridal artistry, soft glam, lessons and curated beauty essentials in Bengaluru.",
                CreateSection("story", "philosophy", "Our philosophy", "Beauty that still looks like you.",
                    "We build looks from skin outward — considered colour, weightless texture and a finish that feels like the best version of you, not a mask. Every chair begins with listening.",
                    "/media/seed/about.jpg"),
                CreateSection("quote", "editorial", "The studio note", "Soft focus. Strong finish.",
                    "A little glow, a lot of intention. Our artists blend editorial technique with the ease of real life.", "/media/seed/studio-1.jpg")),
            CreatePage("about", "Meet the artist behind the brush", "A studio built on warmth, precision and a love for real skin.",
                "About Makeup Art | Bengaluru Makeup Studio", "Meet the artists, values and story behind Makeup Art.",
                CreateSection("intro", "text", "Our story", "A quiet obsession with light, colour and confidence.",
                    "Makeup Art began in a sunlit Indiranagar apartment with one chair, a kit full of carefully chosen textures, and a belief that getting ready should feel as good as the result. Today our studio works with brides, editorial teams and anyone who wants a look that feels unmistakably theirs.",
                    "/media/seed/studio-1.jpg"),
                CreateSection("values", "values", "What we care about", "Skin first. Always.",
                    "We patch-test, customise and never chase a trend at the expense of comfort. Our kit is cruelty-free, our processes are hygiene-led, and our artists are trained to work across skin tones and textures.")),
            CreatePage("services", "Artist-led services, made for your moment", "From bridal mornings to a first lesson in colour.",
                "Makeup Services | Makeup Art", "Bridal makeup, party glam, editorial artistry and personal lessons in Bengaluru.",
                CreateSection("intro", "text", "The service menu", "Your face, your finish, our full attention.",
                    "Every service begins with a consultation. Choose a signature look or let us build something entirely yours.", "/media/seed/service-1.jpg")),
            CreatePage("packages", "Packages for the whole story", "Trials, touch-ups and the people beside you.",
                "Makeup Packages | Makeup Art", "Bridal and occasion makeup packages with trials, touch-ups and guest artistry.",
                CreateSection("intro", "text", "The package edit", "Every detail, considered.",
                    "Our packages bundle artistry, trials and finishing touches so you can stay present on the day that matters.", "/media/seed/package-1.jpg")),
            CreatePage("products", "The studio edit", "Makeup bag essentials chosen by our artists.",
                "Shop Makeup Art | Curated Beauty Essentials", "Shop lipsticks, complexion, eye and skincare essentials selected by our artists.",
                CreateSection("intro", "text", "The shelf", "Tried, tested and in the kit.",
                    "A tight edit of textures we reach for on set and at the studio. Small-batch, cruelty-free and chosen for real skin.", "/media/seed/product-1.jpg")),
            CreatePage("portfolio", "A gallery of transformations", "Bridal, editorial and everything in between.",
                "Portfolio | Makeup Art", "Explore bridal and editorial makeup work by Makeup Art.",
                CreateSection("intro", "text", "The portfolio", "Quiet detail. Unforgettable faces.",
                    "A selection of recent work. Filter by the occasion, save what speaks to you, and bring it to your consultation.")),
            CreatePage("vlogs", "The Makeup Art journal", "Tutorials, studio diaries and conversations with people we love.",
                "Vlogs & Studio Journal | Makeup Art", "Watch makeup tutorials, influencer conversations and studio diaries.",
                CreateSection("intro", "text", "Watch the process", "Behind the brush.",
                    "Long-form conversations, practical tutorials and the occasional studio chaos — all filmed with our community.")),
            CreatePage("testimonials", "Love notes from the chair", "The words we keep coming back to.",
                "Testimonials | Makeup Art", "Read what clients say about their experience at Makeup Art.",
                CreateSection("intro", "text", "Client stories", "The best part of the job.",
                    "Real reflections from brides, editorial clients and lesson students.")),
            CreatePage("faq", "Ask us anything", "Practical answers before you book.",
                "FAQ | Makeup Art", "Answers about bookings, products, payments, shipping and classes.",
                CreateSection("intro", "text", "Good to know", "The details, in one place.",
                    "Still curious? Send us a note and a real person will reply.")),
            CreatePage("contact", "Come see us", "Coffee, colour and a conversation about your look.",
                "Contact & Location | Makeup Art", "Visit our Bengaluru studio or contact the Makeup Art team.",
                CreateSection("intro", "text", "The studio", "Find us in Indiranagar.",
                    "Walk-ins are welcome for product pickups. Artistry appointments are by booking only.", "/media/seed/studio-1.jpg")),
            CreatePage("privacy", "Privacy & your data", "How we collect, use and protect your information.",
                "Privacy Policy | Makeup Art", "Makeup Art privacy policy.",
                CreateSection("body", "legal", "Privacy", "Your privacy matters.",
                    "We collect the information needed to provide services, fulfil orders and support you. We never sell personal data. Payment card details are handled by our PCI-compliant payment partner and never stored on our servers. You can request access, correction or deletion of your data by emailing hello@makeupart.local.")),
            CreatePage("terms", "Terms of service", "The agreement that keeps things clear.",
                "Terms | Makeup Art", "Makeup Art terms of service.",
                CreateSection("body", "legal", "Terms", "The practical agreement.",
                    "Bookings are confirmed after a deposit. Cancellations within 48 hours may retain the deposit. Product colours are photographed as accurately as possible but screens vary. Offers cannot be combined unless stated. All content and imagery on this site belongs to Makeup Art unless credited otherwise.")),
            CreatePage("shipping", "Shipping & delivery", "Where your beauty edit travels.",
                "Shipping | Makeup Art", "Shipping and delivery information for Makeup Art orders.",
                CreateSection("body", "legal", "Shipping", "Packed with care.",
                    "Orders are dispatched within 1–2 business days from Bengaluru. Standard delivery takes 3–5 business days; express delivery takes 1–2 business days. Tracking is emailed once your parcel leaves the studio. Free standard shipping applies above ₹2,500.")),
            CreatePage("returns", "Returns & refunds", "Fair, simple and human.",
                "Returns & Refunds | Makeup Art", "Return and refund policy for Makeup Art.",
                CreateSection("body", "legal", "Returns", "A considered policy.",
                    "Unopened products can be returned within 7 days of delivery. Hygiene-sensitive items such as opened lipsticks, mascaras and used tools cannot be returned unless faulty. Services and packages are non-refundable once the appointment begins. To start a return, contact care@makeupart.local with your order number."))
        };
        db.Pages.AddRange(pages);

        db.HeroSlides.AddRange(
            new HeroSlide
            {
                Eyebrow = "Bridal artistry · Bengaluru",
                Title = "The look you'll remember.",
                Subtitle = "Soft-focus skin, colour with intention, and a finish that feels like you — for the moments that deserve a little more.",
                ModelImageUrl = "/media/seed/hero-1.jpg",
                BackgroundImageUrl = "/media/seed/hero-bg-1.jpg",
                CtaLabel = "Explore bridal",
                CtaUrl = "/packages",
                SecondaryCtaLabel = "View portfolio",
                SecondaryCtaUrl = "/portfolio",
                AccentColor = "#E9C8C1",
                SecondaryColor = "#C9A26B",
                EffectJson = "{\"intensity\":0.8,\"palette\":\"champagne\"}",
                SortOrder = 1
            },
            new HeroSlide
            {
                Eyebrow = "The studio edit",
                Title = "Colour that moves with you.",
                Subtitle = "A curated kit of textures for real skin, real light and the long way home.",
                ModelImageUrl = "/media/seed/hero-2.jpg",
                BackgroundImageUrl = "/media/seed/hero-bg-2.jpg",
                CtaLabel = "Shop the edit",
                CtaUrl = "/products",
                SecondaryCtaLabel = "Our services",
                SecondaryCtaUrl = "/services",
                AccentColor = "#D9B8A6",
                SecondaryColor = "#8E4A5B",
                EffectJson = "{\"intensity\":0.65,\"palette\":\"rose\"}",
                SortOrder = 2
            },
            new HeroSlide
            {
                Eyebrow = "Learn with us",
                Title = "Your features, your rules.",
                Subtitle = "Private lessons that decode the kit, the tools and the five-minute version of your favourite look.",
                ModelImageUrl = "/media/seed/hero-3.jpg",
                BackgroundImageUrl = "/media/seed/hero-bg-3.jpg",
                CtaLabel = "Book a lesson",
                CtaUrl = "/services",
                SecondaryCtaLabel = "About the studio",
                SecondaryCtaUrl = "/about",
                AccentColor = "#E4C6B8",
                SecondaryColor = "#7A3F50",
                EffectJson = "{\"intensity\":0.7,\"palette\":\"bronze\"}",
                SortOrder = 3
            });

        db.SocialLinks.AddRange(
            new SocialLink { Platform = "Instagram", Url = "https://instagram.com", Icon = "instagram", SortOrder = 1 },
            new SocialLink { Platform = "Pinterest", Url = "https://pinterest.com", Icon = "pinterest", SortOrder = 2 },
            new SocialLink { Platform = "YouTube", Url = "https://youtube.com", Icon = "youtube", SortOrder = 3 },
            new SocialLink { Platform = "Facebook", Url = "https://facebook.com", Icon = "facebook", SortOrder = 4 });

        db.MusicTracks.AddRange(
            new MusicTrack { Title = "Valley Sunset", Artist = "Alejandro Magaña (A. M.)", Url = "/media/seed/music-1.mp3", DurationSeconds = 134, IsDefault = true, SortOrder = 1, Credit = "Mixkit Free Stock Music License · https://mixkit.co/free-stock-music/ambient/" },
            new MusicTrack { Title = "Forest Mist Whispers", Artist = "Alejandro Magaña (A. M.)", Url = "/media/seed/music-2.mp3", DurationSeconds = 151, SortOrder = 2, Credit = "Mixkit Free Stock Music License · https://mixkit.co/free-stock-music/ambient/" },
            new MusicTrack { Title = "Relaxation 05", Artist = "Lily J", Url = "/media/seed/music-3.mp3", DurationSeconds = 118, SortOrder = 3, Credit = "Mixkit Free Stock Music License · https://mixkit.co/free-stock-music/ambient/" });

        db.Banners.AddRange(
            new Banner { Title = "Bridal preview days", Subtitle = "Book a trial this month and receive a complimentary touch-up kit.", ImageUrl = "/media/seed/package-1.jpg", LinkUrl = "/packages", Placement = "home", SortOrder = 1 },
            new Banner { Title = "The glow edit", Subtitle = "Complexion favourites, selected by our artists.", ImageUrl = "/media/seed/product-3.jpg", LinkUrl = "/products", Placement = "products", SortOrder = 2 });

        db.Testimonials.AddRange(
            CreateTestimonial("Ananya & Rohan", "Bride · Bengaluru", "I cried happy tears twice — and the makeup did not move once. Aarohi understood exactly what I meant by 'still me'.", 5, "/media/seed/testimonial-1.jpg", true, 1),
            CreateTestimonial("Meera Kapoor", "Editorial client", "The softest skin I have ever seen in photographs. The studio feels calm even when the schedule is chaos.", 5, "/media/seed/testimonial-2.jpg", true, 2),
            CreateTestimonial("Nisha Thomas", "Bridal trial", "I came in with a Pinterest board and left with something better. The trial alone was worth it.", 5, "/media/seed/testimonial-3.jpg", true, 3),
            CreateTestimonial("Ritika Shah", "Personal lesson", "I finally know what to do with my hooded eyes. Practical, patient and genuinely fun.", 5, null, true, 4),
            CreateTestimonial("Sana Ali", "Reception", "The reception glow lasted through dinner, dancing and a very emotional speech. Ten out of ten.", 5, "/media/seed/testimonial-4.jpg", false, 5),
            CreateTestimonial("Aditi Menon", "Bride", "Every artist made me feel heard. The photographs are my favourite thing we own.", 5, null, false, 6));

        db.Faqs.AddRange(
            CreateFaq("How far in advance should I book bridal makeup?", "We recommend 6–9 months for peak wedding dates, and 8–12 weeks for a trial. If your date is sooner, message us — we often hold a little space for last-minute celebrations.", "Bridal", 1),
            CreateFaq("Do you travel to the venue?", "Yes. We travel across Bengaluru and can arrange destination travel. Travel and stay are quoted separately based on location and call time.", "Bridal", 2),
            CreateFaq("What does a bridal trial include?", "A 2–3 hour consultation, skin prep, one full look, photographs in natural and studio light, and a written plan of the products and touch-up steps for the day.", "Bridal", 3),
            CreateFaq("Is the makeup waterproof?", "We use long-wear, humidity-conscious formulas and set the look for your environment. Every kit is adjusted for skin type and the season.", "Services", 4),
            CreateFaq("Do you work with all skin tones?", "Absolutely. Our artists train across deep, medium and fair skin tones, undertones and textures. Your trial is the place to fine-tune colour.", "Services", 5),
            CreateFaq("Can I order products online?", "Yes. Add to bag, choose standard or express delivery, and pay securely with cards, UPI, netbanking or wallets through Razorpay. GST is calculated at checkout.", "Orders", 6),
            CreateFaq("How do I track my order?", "You'll receive a tracking link by email once your parcel leaves the studio. You can also find order updates in your account.", "Orders", 7),
            CreateFaq("What is your return policy?", "Unopened products can be returned within 7 days. Opened hygiene-sensitive products, services and packages are non-refundable unless faulty. See the returns page for details.", "Orders", 8),
            CreateFaq("Can I reschedule my appointment?", "Yes, with at least 48 hours' notice. We'll do our best to move your date; deposits may be retained for late cancellations.", "Bookings", 9),
            CreateFaq("Do you offer gift cards?", "We do. Contact the studio and we'll create a digital gift card for services, lessons or products.", "General", 10));

        var portfolio = new[]
        {
            ("Soft focus bride", "Bridal", "/media/seed/portfolio-1.jpg"),
            ("Champagne reception", "Bridal", "/media/seed/portfolio-2.jpg"),
            ("Modern maharani", "Bridal", "/media/seed/portfolio-3.jpg"),
            ("Editorial bronze", "Editorial", "/media/seed/portfolio-4.jpg"),
            ("Glass skin study", "Editorial", "/media/seed/portfolio-5.jpg"),
            ("Party in plum", "Occasion", "/media/seed/portfolio-6.jpg"),
            ("Sunlit engagement", "Occasion", "/media/seed/portfolio-7.jpg"),
            ("The lesson look", "Lessons", "/media/seed/portfolio-8.jpg")
        };
        var order = 1;
        foreach (var (title, category, image) in portfolio)
        {
            db.PortfolioItems.Add(new PortfolioItem
            {
                Title = title,
                Category = category,
                Description = "A recent Makeup Art studio look.",
                ImageUrl = image,
                IsFeatured = order <= 4,
                TagsCsv = "soft glam,studio,real skin",
                SortOrder = order++
            });
        }

        db.Vlogs.Add(
            new Vlog { Title = "Makeup application at the studio", Slug = "makeup-application-at-the-studio", Description = "A short look at a makeup artist applying a soft, polished finish for a client.", Type = VlogType.Studio, ThumbnailUrl = "/media/seed/vlog-1.jpg", VideoUrl = "/media/seed/vlog-studio.mp4", DurationSeconds = 15, InfluencerName = "Makeup Art Studio", IsFeatured = true, SortOrder = 1, PublishedAtUtc = DateTime.UtcNow.AddDays(-4) });

        db.EmailTemplates.AddRange(
            new EmailTemplate { Key = "welcome", Subject = "Welcome to Makeup Art", BodyHtml = "<h1>Welcome, beautiful.</h1><p>We're so glad you're here. Your account is ready.</p>", Description = "Sent after sign-up." },
            new EmailTemplate { Key = "order_confirmation", Subject = "Your Makeup Art order {{orderNumber}}", BodyHtml = "<h1>Thank you.</h1><p>We're preparing order {{orderNumber}} with care.</p>", Description = "Order receipt." },
            new EmailTemplate { Key = "password_reset", Subject = "Reset your Makeup Art password", BodyHtml = "<p>Use the link below to choose a new password.</p>", Description = "Password reset." });

        await db.SaveChangesAsync(ct);

        var defaultMusic = await db.MusicTracks.OrderBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
        if (defaultMusic is not null)
        {
            var settings = await db.SiteSettings.FirstAsync(ct);
            settings.DefaultMusicTrackId = defaultMusic.Id;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureCatalogAsync(CancellationToken ct)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var gst18 = new TaxRate { Name = "GST 18% · cosmetics", RatePercent = 18m, HsnCode = "3304", IsDefault = true, Description = "Standard GST for cosmetic products." };
        var gst18Services = new TaxRate { Name = "GST 18% · services", RatePercent = 18m, HsnCode = "9997", Description = "Makeup and beauty services." };
        var gst12 = new TaxRate { Name = "GST 12% · skincare", RatePercent = 12m, HsnCode = "3304", Description = "Skincare essentials." };
        db.TaxRates.AddRange(gst18, gst18Services, gst12);
        await db.SaveChangesAsync(ct);

        var lips = new Category { Name = "Lips", Slug = "lips", Description = "Colour, comfort and a little ceremony.", ImageUrl = "/media/seed/category-lips.jpg", SortOrder = 1 };
        var complexion = new Category { Name = "Complexion", Slug = "complexion", Description = "Skin that looks like skin.", ImageUrl = "/media/seed/category-complexion.jpg", SortOrder = 2 };
        var eyes = new Category { Name = "Eyes", Slug = "eyes", Description = "Soft definition to full drama.", ImageUrl = "/media/seed/category-eyes.jpg", SortOrder = 3 };
        var skincare = new Category { Name = "Skincare", Slug = "skincare", Description = "The prep beneath the paint.", ImageUrl = "/media/seed/category-skincare.jpg", SortOrder = 4 };
        db.Categories.AddRange(lips, complexion, eyes, skincare);
        await db.SaveChangesAsync(ct);

        var products = new List<Product>
        {
            CreateProduct("Velvet Veil Lipstick", "velvet-veil-lipstick", "MA-LIP-001", "A weightless velvet lip with a blurred, soft-focus finish.", "Comfortable pigment that wears like a second skin. Build one layer for a stain or three for a statement.", lips, 1290m, 1590m, gst18, "/media/seed/product-1.jpg", 48, tags: "lipstick,matte,velvet,bestseller", featured: true, promoted: true, variants: new[] { ("Rosewood", "MA-LIP-001-RW", 1290m, 18), ("Brick", "MA-LIP-001-BR", 1290m, 12), ("Plum", "MA-LIP-001-PL", 1290m, 18) }),
            CreateProduct("Dew Drop Lip Oil", "dew-drop-lip-oil", "MA-LIP-002", "Glass-shine hydration with a whisper of rose.", "A cushiony oil with jojoba and rosehip that leaves lips glossy, never sticky.", lips, 990m, 1190m, gst18, "/media/seed/product-2.jpg", 61, tags: "lip oil,gloss,hydrating", newArrival: true, variants: new[] { ("Bare", "MA-LIP-002-BA", 990m, 31), ("Rose", "MA-LIP-002-RO", 990m, 30) }),
            CreateProduct("Second Skin Foundation", "second-skin-foundation", "MA-COM-001", "A breathable medium-coverage foundation for real texture.", "Buildable coverage with a satin finish, photographed beautifully in natural light.", complexion, 2490m, 2990m, gst18, "/media/seed/product-3.jpg", 34, tags: "foundation,satin,medium coverage", featured: true, variants: new[] { ("Warm 03", "MA-COM-001-W3", 2490m, 10), ("Neutral 04", "MA-COM-001-N4", 2490m, 12), ("Deep 07", "MA-COM-001-D7", 2490m, 12) }),
            CreateProduct("Soft Focus Compact", "soft-focus-compact", "MA-COM-002", "A blurring powder that sets without flattening.", "Finely milled rice powder and mineral pigments blur pores and shine for a soft, lit-from-within finish.", complexion, 1890m, null, gst18, "/media/seed/product-4.jpg", 27, tags: "powder,setting,blur", featured: true),
            CreateProduct("Rose Quartz Blush", "rose-quartz-blush", "MA-COM-003", "A gel-powder blush that melts into skin.", "A translucent wash of rose that looks like a flush, not a stripe.", complexion, 1590m, 1890m, gst18, "/media/seed/product-5.jpg", 40, tags: "blush,gel powder,rose"),
            CreateProduct("Golden Hour Highlighter", "golden-hour-highlighter", "MA-COM-004", "Liquid light with a grown-up glow.", "A champagne liquid highlighter to tap over the high points for a finish that reads skin, not sparkle.", complexion, 1790m, null, gst18, "/media/seed/product-6.jpg", 32, tags: "highlighter,glow,liquid", promoted: true),
            CreateProduct("Midnight Kohl Liner", "midnight-kohl-liner", "MA-EYE-001", "A soft, smudgeable kohl for quiet drama.", "Creamy, waterproof and easy to blend before it locks in.", eyes, 790m, 990m, gst18, "/media/seed/product-7.jpg", 75, tags: "eyeliner,kohl,waterproof", featured: true),
            CreateProduct("Cloud Volume Mascara", "cloud-volume-mascara", "MA-EYE-002", "Featherlight volume, zero flake.", "A buildable mascara with a tapered brush for lift at the root and separation through the tips.", eyes, 1190m, null, gst18, "/media/seed/product-8.jpg", 58, tags: "mascara,volume,cruelty free", newArrival: true),
            CreateProduct("Sunset Palette", "sunset-palette", "MA-EYE-003", "Nine warm neutrals and two quiet shimmers.", "A studio favourite for soft daytime definition and smoky evening eyes.", eyes, 2990m, 3490m, gst18, "/media/seed/product-9.jpg", 22, tags: "eyeshadow,palette,warm", featured: true, promoted: true),
            CreateProduct("Silk Set Brow Gel", "silk-set-brow-gel", "MA-EYE-004", "Laminated brows, without the salon.", "A flexible clear-hold gel with a precise spoolie for feathered, natural brows.", eyes, 850m, null, gst18, "/media/seed/product-10.jpg", 64, tags: "brow,gel,laminated", newArrival: true),
            CreateProduct("Petal Cream Cleanser", "petal-cream-cleanser", "MA-SKN-001", "A milky cleanse that leaves the barrier happy.", "Dissolves makeup and daily grime without stripping. Fragrance-free and suitable for sensitive skin.", skincare, 1190m, 1390m, gst12, "/media/seed/product-11.jpg", 45, tags: "cleanser,cream,sensitive skin", featured: true),
            CreateProduct("Glow Ritual Face Oil", "glow-ritual-face-oil", "MA-SKN-002", "Overnight nourishment in a few golden drops.", "A blend of squalane, rosehip and vitamin E for a plump, rested morning.", skincare, 1890m, 2190m, gst12, "/media/seed/product-12.jpg", 29, tags: "face oil,glow,hydrating", newArrival: true)
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        var serviceBridal = new ServiceCategory { Name = "Bridal", Slug = "bridal", Description = "The full ritual, from trial to touch-up.", ImageUrl = "/media/seed/service-1.jpg", SortOrder = 1 };
        var serviceOccasion = new ServiceCategory { Name = "Occasion", Slug = "occasion", Description = "Soft glam for celebrations big and small.", ImageUrl = "/media/seed/service-4.jpg", SortOrder = 2 };
        var serviceEditorial = new ServiceCategory { Name = "Editorial", Slug = "editorial", Description = "Camera-ready artistry for campaigns and shoots.", ImageUrl = "/media/seed/service-3.jpg", SortOrder = 3 };
        var serviceLessons = new ServiceCategory { Name = "Lessons", Slug = "lessons", Description = "Learn your face, your kit, your rules.", ImageUrl = "/media/seed/service-6.jpg", SortOrder = 4 };
        db.ServiceCategories.AddRange(serviceBridal, serviceOccasion, serviceEditorial, serviceLessons);
        await db.SaveChangesAsync(ct);

        var services = new List<Service>
        {
            new() { Name = "Bridal Signature Artistry", Slug = "bridal-signature-artistry", Category = serviceBridal, ShortDescription = "The full bridal ritual, designed around you.", Description = "Includes a detailed consultation, skin prep, long-wear bridal makeup, lashes, veil placement and a touch-up plan. Ideal for the bride who wants soft definition and photographs that still feel like her.", Price = 25000m, CompareAtPrice = 28000m, DurationMinutes = 180, ImageUrl = "/media/seed/service-1.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 1 },
            new() { Name = "Reception Glow", Slug = "reception-glow", Category = serviceBridal, ShortDescription = "A luminous, camera-ready reception look.", Description = "A sculpted glow with soft eyes, long-wear finish and a complementary hair styling touch-up. Perfect for the second celebration.", Price = 18000m, DurationMinutes = 120, ImageUrl = "/media/seed/service-2.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 2 },
            new() { Name = "Engagement Soft Glam", Slug = "engagement-soft-glam", Category = serviceBridal, ShortDescription = "Romantic, weightless colour for the yes.", Description = "Soft-focus skin, a wash of blush and defined lashes for engagements, mehendi and intimate ceremonies.", Price = 15000m, DurationMinutes = 120, ImageUrl = "/media/seed/service-3.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 3 },
            new() { Name = "Editorial & Photoshoot", Slug = "editorial-photoshoot", Category = serviceEditorial, ShortDescription = "Concept-led makeup for the camera.", Description = "A collaborative session for campaigns, portraits and editorial stories. Includes look development and on-set touch-ups for the first two hours.", Price = 12000m, DurationMinutes = 90, ImageUrl = "/media/seed/service-4.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 4 },
            new() { Name = "Party Glam", Slug = "party-glam", Category = serviceOccasion, ShortDescription = "Your evening, elevated.", Description = "A polished party look with skin prep, eyes of your choice and a setting ritual that survives dinner and dancing.", Price = 8500m, DurationMinutes = 75, ImageUrl = "/media/seed/service-5.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 5 },
            new() { Name = "Personal Makeup Lesson", Slug = "personal-makeup-lesson", Category = serviceLessons, ShortDescription = "Learn the five-minute version of you.", Description = "A private, hands-on lesson using your kit or ours. We cover base, colour placement, brows and a repeatable everyday routine. Includes a written product guide.", Price = 7500m, DurationMinutes = 120, ImageUrl = "/media/seed/service-6.jpg", IsFeatured = true, TaxRate = gst18Services, SortOrder = 6 }
        };
        db.Services.AddRange(services);
        await db.SaveChangesAsync(ct);

        var pack1 = new ServicePackage
        {
            Name = "The Bridal Heirloom", Slug = "bridal-heirloom", ShortDescription = "The complete bridal story: trial, wedding day and the people beside you.",
            Description = "Designed for the bride who wants a calm, considered morning. Includes a full trial, wedding-day makeup, touch-up kit, and artistry for two guests.",
            Price = 59000m, CompareAtPrice = 68000m, DurationMinutes = 360, ImageUrl = "/media/seed/package-1.jpg", Occasion = "Bridal", IsFeatured = true, TaxRate = gst18Services, SortOrder = 1,
            Items = [new ServicePackageItem { Service = services[0], Quantity = 1, SortOrder = 1 }, new ServicePackageItem { Service = services[2], Quantity = 2, SortOrder = 2 }]
        };
        var pack2 = new ServicePackage
        {
            Name = "Reception Radiance", Slug = "reception-radiance", ShortDescription = "A second look with the same soft, luminous signature.",
            Description = "Reception makeup, a quick hair touch-up and a glow-focused skin ritual for the evening celebration.",
            Price = 32000m, CompareAtPrice = 36000m, DurationMinutes = 180, ImageUrl = "/media/seed/package-2.jpg", Occasion = "Reception", IsFeatured = true, TaxRate = gst18Services, SortOrder = 2,
            Items = [new ServicePackageItem { Service = services[1], Quantity = 1, SortOrder = 1 }]
        };
        var pack3 = new ServicePackage
        {
            Name = "Editorial Muse", Slug = "editorial-muse", ShortDescription = "Concept development and camera-ready artistry for one shoot.",
            Description = "A half-day editorial package with look development, two makeup changes and on-set touch-ups.",
            Price = 24000m, CompareAtPrice = 28000m, DurationMinutes = 240, ImageUrl = "/media/seed/package-3.jpg", Occasion = "Editorial", IsFeatured = true, TaxRate = gst18Services, SortOrder = 3,
            Items = [new ServicePackageItem { Service = services[3], Quantity = 2, SortOrder = 1 }]
        };
        var pack4 = new ServicePackage
        {
            Name = "Soft Glam Soirée", Slug = "soft-glam-soiree", ShortDescription = "You and two friends, ready for the night.",
            Description = "Three party glam appointments, a shared styling session and a glass of something sparkling.",
            Price = 19000m, CompareAtPrice = 22500m, DurationMinutes = 225, ImageUrl = "/media/seed/package-4.jpg", Occasion = "Occasion", IsFeatured = true, TaxRate = gst18Services, SortOrder = 4,
            Items = [new ServicePackageItem { Service = services[4], Quantity = 3, SortOrder = 1 }]
        };
        db.ServicePackages.AddRange(pack1, pack2, pack3, pack4);
        await db.SaveChangesAsync(ct);

        db.ProductCombos.AddRange(
            new ProductCombo
            {
                Name = "The Glow Starter Kit", Slug = "glow-starter-kit", Description = "Second Skin Foundation, Rose Quartz Blush and Golden Hour Highlighter — the studio's base ritual in one edit.",
                Price = 5490m, CompareAtPrice = 5870m, ImageUrl = "/media/seed/product-3.jpg", IsFeatured = true, SortOrder = 1,
                Items = [new ProductComboItem { Product = products[2], Quantity = 1, SortOrder = 1 }, new ProductComboItem { Product = products[4], Quantity = 1, SortOrder = 2 }, new ProductComboItem { Product = products[5], Quantity = 1, SortOrder = 3 }]
            },
            new ProductCombo
            {
                Name = "Lip Wardrobe", Slug = "lip-wardrobe", Description = "Velvet Veil in Rosewood and Dew Drop Lip Oil in Bare — the two-step lip we use on almost everyone.",
                Price = 1990m, CompareAtPrice = 2280m, ImageUrl = "/media/seed/product-1.jpg", IsFeatured = true, SortOrder = 2,
                Items = [new ProductComboItem { Product = products[0], Quantity = 1, SortOrder = 1 }, new ProductComboItem { Product = products[1], Quantity = 1, SortOrder = 2 }]
            },
            new ProductCombo
            {
                Name = "The Eye Edit", Slug = "the-eye-edit", Description = "Midnight Kohl, Cloud Volume Mascara and the Sunset Palette for a complete eye wardrobe.",
                Price = 4490m, CompareAtPrice = 4970m, ImageUrl = "/media/seed/product-9.jpg", IsFeatured = true, SortOrder = 3,
                Items = [new ProductComboItem { Product = products[6], Quantity = 1, SortOrder = 1 }, new ProductComboItem { Product = products[7], Quantity = 1, SortOrder = 2 }, new ProductComboItem { Product = products[8], Quantity = 1, SortOrder = 3 }]
            });

        db.Offers.AddRange(
            new Offer { Title = "The bridal preview", Subtitle = "15% off any package", Description = "Book a trial this month and save on your chosen package.", Code = "BRIDAL15", DiscountType = DiscountType.Percentage, DiscountValue = 15m, ImageUrl = "/media/seed/package-1.jpg", AppliesTo = DiscountAppliesTo.Packages, IsFeatured = true, SortOrder = 1 },
            new Offer { Title = "A little welcome", Subtitle = "10% off your first order", Description = "Your first Makeup Art edit, with a little love.", Code = "WELCOME10", DiscountType = DiscountType.Percentage, DiscountValue = 10m, ImageUrl = "/media/seed/product-2.jpg", AppliesTo = DiscountAppliesTo.Products, IsFeatured = true, SortOrder = 2 },
            new Offer { Title = "Glow kit gift", Subtitle = "On orders above ₹4,999", Description = "A complimentary mini glow ritual with qualifying orders, while stocks last.", ImageUrl = "/media/seed/product-6.jpg", AppliesTo = DiscountAppliesTo.All, IsFeatured = false, SortOrder = 3 });

        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", Description = "10% off your first order.", DiscountType = DiscountType.Percentage, DiscountValue = 10m, MinimumOrderAmount = 999m, MaximumDiscountAmount = 1000m, Scope = CouponScope.Products, UsageLimit = 1000, PerUserLimit = 1 },
            new Coupon { Code = "BRIDAL15", Description = "15% off bridal packages.", DiscountType = DiscountType.Percentage, DiscountValue = 15m, MinimumOrderAmount = 10000m, MaximumDiscountAmount = 8000m, Scope = CouponScope.Packages, UsageLimit = 200, PerUserLimit = 1 },
            new Coupon { Code = "GLOW500", Description = "Flat ₹500 off orders above ₹3,000.", DiscountType = DiscountType.Fixed, DiscountValue = 500m, MinimumOrderAmount = 3000m, Scope = CouponScope.All, UsageLimit = 500, PerUserLimit = 2 });

        db.ShippingMethods.AddRange(
            new ShippingMethod { Name = "Standard delivery", Description = "3–5 business days across India.", Price = 99m, FreeAboveAmount = 2500m, EstimatedDays = "3–5 business days", IsDefault = true, SortOrder = 1 },
            new ShippingMethod { Name = "Express delivery", Description = "1–2 business days in metro cities.", Price = 199m, FreeAboveAmount = 5000m, EstimatedDays = "1–2 business days", SortOrder = 2 });

        db.MediaAssets.AddRange(SeedMedia());

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureDemoOrdersAsync(ApplicationUser customer, CancellationToken ct)
    {
        if (await db.Orders.AnyAsync(ct)) return;
        var products = await db.Products.OrderBy(x => x.SortOrder).Take(12).ToListAsync(ct);
        if (products.Count < 3) return;
        var shipping = await db.ShippingMethods.OrderBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
        var settings = await db.SiteSettings.FirstAsync(ct);
        var address = new { fullName = "Priya Rao", phone = "+91 90000 12345", line1 = "44 Jasmine Residency", city = "Bengaluru", state = "Karnataka", postalCode = "560095", country = "India" };
        var addressJson = JsonSerializer.Serialize(address);
        var now = DateTime.UtcNow;

        var demos = new[]
        {
            (number: "MA-2026-0001", days: 1, status: OrderStatus.Paid, pay: PaymentStatus.Captured, items: new[] { (products[0], 1), (products[2], 1) }),
            (number: "MA-2026-0002", days: 3, status: OrderStatus.Processing, pay: PaymentStatus.Captured, items: new[] { (products[4], 1), (products[5], 1) }),
            (number: "MA-2026-0003", days: 6, status: OrderStatus.Shipped, pay: PaymentStatus.Captured, items: new[] { (products[6], 2), (products[7], 1) }),
            (number: "MA-2026-0004", days: 9, status: OrderStatus.Delivered, pay: PaymentStatus.Captured, items: new[] { (products[8], 1), (products[9], 1) }),
            (number: "MA-2026-0005", days: 13, status: OrderStatus.Delivered, pay: PaymentStatus.Captured, items: new[] { (products[10], 1), (products[11], 1) }),
            (number: "MA-2026-0006", days: 18, status: OrderStatus.Delivered, pay: PaymentStatus.Captured, items: new[] { (products[1], 2), (products[3], 1) }),
            (number: "MA-2026-0007", days: 24, status: OrderStatus.Cancelled, pay: PaymentStatus.Failed, items: new[] { (products[0], 1) }),
            (number: "MA-2026-0008", days: 29, status: OrderStatus.Delivered, pay: PaymentStatus.Captured, items: new[] { (products[9], 2) })
        };

        var seq = 1;
        foreach (var demo in demos)
        {
            var input = demo.items.Select(x => new PricingLineInput(OrderItemType.Product, x.Item1.Id, x.Item1.Name, x.Item1.Sku, x.Item1.ImageUrl, x.Item1.Price, x.Item2, x.Item1.TaxRate?.RatePercent ?? 18m, x.Item1.HsnCode)).ToList();
            var pricing = PricingCalculator.Compute(input, null, demo.status == OrderStatus.Cancelled ? 0m : 0m, true, settings.BusinessState, "Karnataka", settings.Currency);
            var order = new Order
            {
                OrderNumber = demo.number,
                UserId = customer.Id,
                CustomerEmail = customer.Email ?? "",
                CustomerName = customer.DisplayName,
                CustomerPhone = customer.PhoneNumber ?? "",
                Status = demo.status,
                PaymentStatus = demo.pay,
                SubTotal = pricing.SubTotal,
                DiscountAmount = pricing.DiscountAmount,
                ShippingAmount = pricing.ShippingAmount,
                TaxAmount = pricing.TaxAmount,
                TotalAmount = pricing.TotalAmount,
                Currency = "INR",
                ShippingAddressJson = addressJson,
                BillingAddressJson = addressJson,
                ShippingMethodName = shipping?.Name ?? "Standard delivery",
                PlaceOfSupply = "Karnataka",
                IsGstInvoice = true,
                PlacedAtUtc = now.AddDays(-demo.days),
                PaidAtUtc = demo.pay == PaymentStatus.Captured ? now.AddDays(-demo.days) : null,
                StatusHistory = [new OrderStatusHistory { Status = demo.status, Note = "Seeded demo order", ChangedAtUtc = now.AddDays(-demo.days) }]
            };
            foreach (var line in pricing.Lines)
            {
                order.Items.Add(new OrderItem
                {
                    ItemType = OrderItemType.Product,
                    ItemId = line.Input.ItemId,
                    Name = line.Input.Name,
                    Sku = line.Input.Sku,
                    ImageUrl = line.Input.ImageUrl,
                    UnitPrice = line.UnitPrice,
                    Quantity = (int)line.Quantity,
                    TaxRatePercent = line.Input.TaxRatePercent,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal,
                    HsnCode = line.Input.HsnCode
                });
            }
            if (demo.pay == PaymentStatus.Captured)
            {
                order.Payments.Add(new Payment
                {
                    Provider = PaymentProviderType.Mock,
                    ProviderOrderId = $"seed_order_{seq}",
                    ProviderPaymentId = $"seed_payment_{seq}",
                    Amount = pricing.TotalAmount,
                    Currency = "INR",
                    Status = PaymentStatus.Captured,
                    Method = seq % 3 == 0 ? "upi" : "card",
                    PaidAtUtc = now.AddDays(-demo.days),
                    SafeSummary = "Seeded demo payment"
                });
                order.Invoice = new Invoice
                {
                    InvoiceNumber = $"INV-2026-{seq:0000}",
                    InvoiceDateUtc = now.AddDays(-demo.days),
                    GstNumber = settings.GstNumber,
                    PlaceOfSupply = "Karnataka",
                    SubTotal = pricing.SubTotal,
                    DiscountAmount = pricing.DiscountAmount,
                    ShippingAmount = pricing.ShippingAmount,
                    CgstAmount = pricing.CgstAmount,
                    SgstAmount = pricing.SgstAmount,
                    IgstAmount = pricing.IgstAmount,
                    TotalAmount = pricing.TotalAmount
                };
            }
            db.Orders.Add(order);
            seq++;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureExtrasAsync(ApplicationUser customer, CancellationToken ct)
    {
        if (!await db.ProductReviews.AnyAsync(ct))
        {
            var products = await db.Products.OrderBy(x => x.SortOrder).Take(4).ToListAsync(ct);
            var reviews = new[]
            {
                (products.ElementAtOrDefault(0), 5, "My new everyday lip", "Comfortable, not drying, and the Rosewood shade works with everything. Already bought a second."),
                (products.ElementAtOrDefault(1), 4, "Lovely gloss", "Beautiful shine without the sticky feeling. Wish the tube were bigger."),
                (products.ElementAtOrDefault(2), 5, "Actually my skin", "This is the first foundation that hasn't separated on my combination skin by 4pm."),
                (products.ElementAtOrDefault(3), 5, "Studio finish at home", "A little goes a long way. The blur is real but it never looks powdery.")
            };
            foreach (var (product, rating, title, body) in reviews)
            {
                if (product is null) continue;
                db.ProductReviews.Add(new ProductReview
                {
                    ProductId = product.Id,
                    UserId = customer.Id,
                    Rating = rating,
                    Title = title,
                    Body = body,
                    Status = ReviewStatus.Approved,
                    IsVerifiedPurchase = true,
                    OrderId = null
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.BookingRequests.AnyAsync(ct))
        {
            var services = await db.Services.OrderBy(x => x.SortOrder).Take(2).ToListAsync(ct);
            db.BookingRequests.AddRange(
                new BookingRequest { BookingNumber = "BK-2026-0001", FullName = "Sneha Iyer", Email = "sneha@example.com", Phone = "+91 90000 22222", ServiceId = services.FirstOrDefault()?.Id, PreferredDate = DateTime.UtcNow.AddDays(21), PreferredTime = "06:30", Occasion = "Bridal", Notes = "Morning wedding, soft glam, sensitive skin.", Status = BookingStatus.Pending },
                new BookingRequest { BookingNumber = "BK-2026-0002", FullName = "Kavya Menon", Email = "kavya@example.com", Phone = "+91 90000 33333", ServiceId = services.Skip(1).FirstOrDefault()?.Id, PreferredDate = DateTime.UtcNow.AddDays(9), PreferredTime = "14:00", Occasion = "Reception", Status = BookingStatus.Confirmed });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ContactMessages.AnyAsync(ct))
        {
            db.ContactMessages.AddRange(
                new ContactMessage { Name = "Riya Nair", Email = "riya@example.com", Phone = "+91 90000 44444", Subject = "Bridal availability", Message = "Hello! Are you available for a December wedding in Kochi? Soft glam, four guests.", Status = ContactStatus.New },
                new ContactMessage { Name = "Tanvi Desai", Email = "tanvi@example.com", Subject = "Product shade help", Message = "Could you help me choose a foundation shade for neutral undertones?", Status = ContactStatus.Read });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.SupportTickets.AnyAsync(ct))
        {
            var ticket = new SupportTicket
            {
                TicketNumber = "TKT-2026-0001",
                UserId = customer.Id,
                Subject = "Delivery timeline for my order",
                Category = "Order",
                Priority = TicketPriority.Normal,
                Status = TicketStatus.Open,
                LastMessageAtUtc = DateTime.UtcNow.AddHours(-20),
                Messages =
                [
                    new SupportMessage { SenderUserId = customer.Id, SenderName = customer.DisplayName, SenderType = SenderType.Customer, Body = "Hi! I ordered the Glow Starter Kit two days ago. Could you let me know when it might arrive?" }
                ]
            };
            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AnalyticsEvents.AnyAsync(ct))
        {
            var random = new Random(42);
            var paths = new[] { "/", "/services", "/packages", "/products", "/portfolio", "/vlogs", "/testimonials", "/faq", "/contact", "/products/velvet-veil-lipstick", "/products/second-skin-foundation", "/packages/bridal-heirloom" };
            var events = new List<AnalyticsEvent>();
            for (var day = 29; day >= 0; day--)
            {
                var count = 12 + random.Next(30);
                for (var i = 0; i < count; i++)
                {
                    var path = paths[random.Next(paths.Length)];
                    var type = random.Next(10) switch { 0 or 1 or 2 => "product_view", 3 => "add_to_cart", 4 => "begin_checkout", _ => "page_view" };
                    events.Add(new AnalyticsEvent
                    {
                        EventType = type,
                        Path = path,
                        SessionId = $"seed-{day}-{i / 4}",
                        DeviceType = random.Next(3) == 0 ? "mobile" : "desktop",
                        Referrer = random.Next(4) == 0 ? "https://instagram.com" : null,
                        OccurredAtUtc = DateTime.UtcNow.AddDays(-day).AddMinutes(random.Next(0, 720))
                    });
                }
            }
            db.AnalyticsEvents.AddRange(events);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CustomerAddresses.AnyAsync(ct))
        {
            db.CustomerAddresses.Add(new CustomerAddress
            {
                UserId = customer.Id,
                Label = "Home",
                FullName = customer.DisplayName,
                Phone = customer.PhoneNumber ?? "",
                AddressLine1 = "44 Jasmine Residency",
                AddressLine2 = "Indiranagar",
                City = "Bengaluru",
                State = "Karnataka",
                PostalCode = "560095",
                IsDefaultShipping = true,
                IsDefaultBilling = true
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private static Page CreatePage(string key, string title, string subtitle, string seoTitle, string seoDescription, params ContentSection[] sections)
        => new() { Key = key, Title = title, Subtitle = subtitle, SeoTitle = seoTitle, SeoDescription = seoDescription, Sections = sections.ToList() };

    private static ContentSection CreateSection(string key, string type, string eyebrow, string heading, string body, string? media = null)
        => new() { Key = key, SectionType = type, Eyebrow = eyebrow, Heading = heading, BodyHtml = body, MediaUrl = media, IsActive = true };

    private static Testimonial CreateTestimonial(string name, string role, string quote, int rating, string? image, bool featured, int order)
        => new() { CustomerName = name, Role = role, Quote = quote, Rating = rating, ImageUrl = image, IsFeatured = featured, Status = TestimonialStatus.Approved, SortOrder = order };

    private static Faq CreateFaq(string question, string answer, string category, int order)
        => new() { Question = question, Answer = answer, Category = category, SortOrder = order };

    private static Product CreateProduct(string name, string slug, string sku, string shortDescription, string description, Category category, decimal price, decimal? compare, TaxRate tax, string image, int stock, string? tags = null, bool featured = false, bool promoted = false, bool newArrival = false, IEnumerable<(string Name, string Sku, decimal Price, int Stock)>? variants = null)
    {
        var product = new Product
        {
            Name = name,
            Slug = slug,
            Sku = sku,
            ShortDescription = shortDescription,
            Description = description,
            Category = category,
            Price = price,
            CompareAtPrice = compare,
            TaxRate = tax,
            ImageUrl = image,
            StockQuantity = stock,
            TagsCsv = tags,
            IsFeatured = featured,
            IsPromoted = promoted,
            IsNewArrival = newArrival,
            SeoTitle = $"{name} | Makeup Art",
            SeoDescription = shortDescription
        };
        if (variants is not null)
        {
            var order = 1;
            foreach (var variant in variants)
                product.Variants.Add(new ProductVariant { Name = variant.Name, Sku = variant.Sku, PriceOverride = variant.Price, StockQuantity = variant.Stock, SortOrder = order++ });
        }
        product.Images.Add(new ProductImage { Url = image, AltText = name, SortOrder = 1 });
        return product;
    }

    private static IEnumerable<MediaAsset> SeedMedia()
    {
        var images = new[]
        {
            ("logo.svg", "logo", "Makeup Art monogram", "brand"), ("favicon.svg", "favicon", "Makeup Art favicon", "brand"),
            ("hero-1.jpg", "hero", "Bridal beauty portrait", "hero"), ("hero-2.jpg", "hero", "Glow beauty portrait", "hero"), ("hero-3.jpg", "hero", "Editorial beauty portrait", "hero"),
            ("hero-bg-1.jpg", "hero", "Soft champagne background", "hero"), ("hero-bg-2.jpg", "hero", "Rose background", "hero"), ("hero-bg-3.jpg", "hero", "Bronze background", "hero"),
            ("about.jpg", "about", "Inside the Makeup Art studio", "editorial"), ("studio-1.jpg", "studio", "Makeup Art studio detail", "editorial"),
            ("category-lips.jpg", "lips", "Lip colour category", "catalog"), ("category-complexion.jpg", "complexion", "Complexion category", "catalog"),
            ("category-eyes.jpg", "eyes", "Eye category", "catalog"), ("category-skincare.jpg", "skincare", "Skincare category", "catalog"),
            ("package-1.jpg", "bridal", "Bridal package", "catalog"), ("package-2.jpg", "reception", "Reception package", "catalog"),
            ("package-3.jpg", "editorial", "Editorial package", "catalog"), ("package-4.jpg", "party", "Party package", "catalog"),
            ("service-1.jpg", "bridal", "Bridal makeup service", "catalog"), ("service-2.jpg", "bridal", "Reception makeup service", "catalog"),
            ("service-3.jpg", "engagement", "Engagement makeup service", "catalog"), ("service-4.jpg", "editorial", "Editorial makeup service", "catalog"),
            ("service-5.jpg", "party", "Party makeup service", "catalog"), ("service-6.jpg", "lesson", "Makeup lesson", "catalog"),
            ("testimonial-1.jpg", "bride", "Client portrait", "testimonials"), ("testimonial-2.jpg", "editorial", "Client portrait", "testimonials"),
            ("testimonial-3.jpg", "bride", "Client portrait", "testimonials"), ("testimonial-4.jpg", "reception", "Client portrait", "testimonials"),
            ("vlog-1.jpg", "bridal", "Makeup application video cover", "vlogs")
        };
        for (var i = 1; i <= 8; i++)
            images = [.. images, ($"portfolio-{i}.jpg", $"portfolio {i}", $"Portfolio look {i}", "portfolio")];
        for (var i = 1; i <= 12; i++)
            images = [.. images, ($"product-{i}.jpg", $"product {i}", $"Makeup Art product {i}", "products")];

        foreach (var (file, folder, alt, category) in images)
        {
            yield return new MediaAsset
            {
                FileName = file,
                OriginalFileName = file,
                Url = $"/media/seed/{file}",
                MediaType = MediaType.Image,
                ContentType = file.EndsWith(".svg") ? "image/svg+xml" : "image/jpeg",
                SizeBytes = 0,
                AltText = alt,
                Folder = category,
                Credit = file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                    ? "Editable Makeup Art placeholder brand asset."
                    : PhotoCredit(file),
                IsPublic = true
            };
        }
        for (var i = 1; i <= 3; i++)
        {
            yield return new MediaAsset
            {
                FileName = $"music-{i}.mp3",
                OriginalFileName = $"music-{i}.mp3",
                Url = $"/media/seed/music-{i}.mp3",
                MediaType = MediaType.Audio,
                ContentType = "audio/mpeg",
                SizeBytes = 0,
                AltText = $"Instrumental track {i}",
                Folder = "music",
                Credit = i switch
                {
                    1 => "Valley Sunset · Alejandro Magaña (A. M.) · Mixkit Free Stock Music License",
                    2 => "Forest Mist Whispers · Alejandro Magaña (A. M.) · Mixkit Free Stock Music License",
                    _ => "Relaxation 05 · Lily J · Mixkit Free Stock Music License"
                },
                IsPublic = true
            };
        }

        yield return new MediaAsset
        {
            FileName = "vlog-studio.mp4",
            OriginalFileName = "vlog-studio.mp4",
            Url = "/media/seed/vlog-studio.mp4",
            MediaType = MediaType.Video,
            ContentType = "video/mp4",
            SizeBytes = 0,
            DurationSeconds = 15,
            AltText = "A makeup artist applying makeup to a client in a salon",
            Folder = "vlogs",
            Credit = "Video: Antoni Shkraba / Pexels · https://www.pexels.com/video/woman-applying-makeup-on-a-client-7987790/ (Pexels License)",
            IsPublic = true
        };
    }

    private static string PhotoCredit(string file) => file switch
    {
        "hero-1.jpg" or "testimonial-1.jpg" or "testimonial-3.jpg" or "vlog-1.jpg" or "package-1.jpg" or "service-1.jpg" or "portfolio-1.jpg" or "portfolio-2.jpg" => "Photo: Bonaventure Fernandez / Pexels · https://www.pexels.com/photo/bride-getting-her-makeup-done-for-the-wedding-17499712/ (Pexels License)",
        "hero-2.jpg" or "package-2.jpg" or "service-2.jpg" or "portfolio-3.jpg" or "portfolio-4.jpg" => "Photo: PIXO / Pexels · https://www.pexels.com/photo/bride-being-prepared-by-make-up-artists-14484717/ (Pexels License)",
        "hero-3.jpg" or "about.jpg" or "vlog-3.jpg" or "service-4.jpg" or "portfolio-5.jpg" or "portfolio-6.jpg" or "testimonial-2.jpg" => "Photo: Luis Becerra Fotógrafo / Pexels · https://www.pexels.com/photo/a-woman-putting-makeup-on-her-client-5732998/ (Pexels License)",
        "studio-1.jpg" or "service-3.jpg" or "service-5.jpg" or "service-6.jpg" or "vlog-2.jpg" or "vlog-4.jpg" or "vlog-6.jpg" or "testimonial-4.jpg" or "portfolio-7.jpg" or "portfolio-8.jpg" => "Photo: ishola S. Oladimeji / Pexels · https://www.pexels.com/photo/makeup-artist-doing-makeup-of-a-woman-11041338/ (Pexels License)",
        "hero-bg-1.jpg" or "hero-bg-3.jpg" or "category-complexion.jpg" or "category-skincare.jpg" or "product-1.jpg" or "product-3.jpg" or "product-5.jpg" or "product-7.jpg" or "product-9.jpg" or "product-11.jpg" => "Photo: MART PRODUCTION / Pexels · https://www.pexels.com/photo/beauty-and-make-up-products-on-white-table-7290715/ (Pexels License)",
        "hero-bg-2.jpg" or "category-lips.jpg" or "category-eyes.jpg" or "package-3.jpg" or "package-4.jpg" or "product-2.jpg" or "product-4.jpg" or "product-6.jpg" or "product-8.jpg" or "product-10.jpg" or "product-12.jpg" => "Photo: Ayodeji Fatunla / Pexels · https://www.pexels.com/photo/makeup-products-9846610/ (Pexels License)",
        _ => "Photo: curated royalty-free seed media; see MEDIA_ATTRIBUTION.md."
    };
}
