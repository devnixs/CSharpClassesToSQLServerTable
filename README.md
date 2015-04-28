# CSharpClassesToSQLServerTable
Very simple project that generates sql server tables based on the C# classes provided

# How to use : 

Insert this for example as a C# class :

using System.ComponentModel.DataAnnotations;

```
namespace Data.Entities
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }
        public string Name { get; set; }
        public int StatusId { get; set; }

    }
}
```

Press generate!

Thanks to http://createschema.codeplex.com/ for their help
