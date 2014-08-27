﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DataLayer.DataClasses;
using DataLayer.DataClasses.Concrete;
using GenericServices;
using GenericServices.Core;

namespace ServiceLayer.CourseServices
{
    public class CourseDetailDto : EfGenericDto<Course, CourseDetailDto>
    {

        [UIHint("HiddenInput")]
        public int CourseId { get; set; }

        [MaxLength(255)]
        [Required]
        public string Name { get; set; }

        [MaxLength(255)]
        [Required]
        public string MainPresenter { get; set; }

        [MaxLength(2048)]
        [Required]
        public string Description { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        public int LengthDays { get; set; }

        [DisplayName("Attendees")]
        public string AttendeesNames { get; private set; }

        protected override ServiceFunctions SupportedFunctions
        {
            get { return ServiceFunctions.AllCrudButList | ServiceFunctions.DoesNotNeedSetup; }
        }

        /// <summary>
        /// We need to override this because we must only read the attendess name and not select the other
        /// properties in attendees as they have sql security controls on their columns
        /// </summary>
        /// <param name="context"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        protected override ISuccessOrErrors<CourseDetailDto> CreateDtoAndCopyDataIn(IDbContextWithValidation context, Expression<Func<Course, bool>> predicate)
        {
            Mapper.CreateMap<Course, CourseDetailDto>();
            var status = GetDataUntracked(context).Where(predicate).Project().To<CourseDetailDto>().TrySingleWithPermissionChecking();
            if (!status.IsValid)
                return status;

            //now we just read the name beacause the other columns are protected 
            //(need to use TryManyWithPermissionChecking as not logged in cannot see anything)
            var localStatus =
                context.Set<Attendee>().Where(x => x.CourseId == status.Result.CourseId).Select( x => x.FullName).TryManyWithPermissionChecking();
            if (!localStatus.IsValid)
                return status.SetErrors(localStatus.Errors.Select( x => x.ErrorMessage));

            status.Result.AttendeesNames = string.Join(", ", localStatus.Result);

            return status;
        }
    }
}
