﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NewPlatform.Flexberry.ORM.ODataService.Tests
{
    using System;
    using System.Xml;
    using ICSSoft.STORMNET;
    
    
    // *** Start programmer edit section *** (Using statements)

    // *** End programmer edit section *** (Using statements)


    /// <summary>
    /// Person.
    /// </summary>
    // *** Start programmer edit section *** (Person CustomAttributes)

    // *** End programmer edit section *** (Person CustomAttributes)
    [PublishName("Person")]
    [AutoAltered()]
    [AccessType(ICSSoft.STORMNET.AccessType.none)]
    public class Person : ICSSoft.STORMNET.DataObject
    {
        
        private string fName;
        
        private NewPlatform.Flexberry.ORM.ODataService.Tests.DetailArrayOfChild fChildrens;
        
        // *** Start programmer edit section *** (Person CustomMembers)

        // *** End programmer edit section *** (Person CustomMembers)

        
        /// <summary>
        /// Name.
        /// </summary>
        // *** Start programmer edit section *** (Person.Name CustomAttributes)

        // *** End programmer edit section *** (Person.Name CustomAttributes)
        [StrLen(255)]
        public virtual string Name
        {
            get
            {
                // *** Start programmer edit section *** (Person.Name Get start)

                // *** End programmer edit section *** (Person.Name Get start)
                string result = this.fName;
                // *** Start programmer edit section *** (Person.Name Get end)

                // *** End programmer edit section *** (Person.Name Get end)
                return result;
            }
            set
            {
                // *** Start programmer edit section *** (Person.Name Set start)

                // *** End programmer edit section *** (Person.Name Set start)
                this.fName = value;
                // *** Start programmer edit section *** (Person.Name Set end)

                // *** End programmer edit section *** (Person.Name Set end)
            }
        }
        
        /// <summary>
        /// Person.
        /// </summary>
        // *** Start programmer edit section *** (Person.Childrens CustomAttributes)

        // *** End programmer edit section *** (Person.Childrens CustomAttributes)
        [TypeUsage(new System.Type[] {
                typeof(NewPlatform.Flexberry.ORM.ODataService.Tests.Daughter),
                typeof(NewPlatform.Flexberry.ORM.ODataService.Tests.Son)})]
        public virtual NewPlatform.Flexberry.ORM.ODataService.Tests.DetailArrayOfChild Childrens
        {
            get
            {
                // *** Start programmer edit section *** (Person.Childrens Get start)

                // *** End programmer edit section *** (Person.Childrens Get start)
                if ((this.fChildrens == null))
                {
                    this.fChildrens = new NewPlatform.Flexberry.ORM.ODataService.Tests.DetailArrayOfChild(this);
                }
                NewPlatform.Flexberry.ORM.ODataService.Tests.DetailArrayOfChild result = this.fChildrens;
                // *** Start programmer edit section *** (Person.Childrens Get end)

                // *** End programmer edit section *** (Person.Childrens Get end)
                return result;
            }
            set
            {
                // *** Start programmer edit section *** (Person.Childrens Set start)

                // *** End programmer edit section *** (Person.Childrens Set start)
                this.fChildrens = value;
                // *** Start programmer edit section *** (Person.Childrens Set end)

                // *** End programmer edit section *** (Person.Childrens Set end)
            }
        }
    }
}
