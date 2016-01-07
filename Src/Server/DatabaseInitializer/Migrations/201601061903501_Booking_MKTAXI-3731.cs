namespace apcurium.MK.Booking.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Booking_MKTAXI3731 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "Booking.ConfigurationChangeEntry",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        AccountId = c.String(),
                        AccountEmail = c.String(),
                        Date = c.DateTime(nullable: false),
                        OldValues = c.String(),
                        NewValues = c.String(),
                        Type = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("Booking.ConfigurationChangeEntry");
        }
    }
}
