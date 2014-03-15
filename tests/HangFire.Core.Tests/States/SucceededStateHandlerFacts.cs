﻿using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class SucceededStateHandlerFacts
    {
        private const string JobId = "1";

        private readonly StateApplyingContext _context;
        private readonly Mock<IWriteOnlyPersistentJob> _jobsMock
            = new Mock<IWriteOnlyPersistentJob>();
        private readonly Mock<IWriteOnlyPersistentCounter> _countersMock
            = new Mock<IWriteOnlyPersistentCounter>();
        
        public SucceededStateHandlerFacts()
        {
            var methodInfo = typeof(SucceededStateHandlerFacts)
                .GetMethod("TestMethod");
            var jobMethod = new JobMethod(typeof(SucceededStateHandlerFacts), methodInfo);
            var stateContext = new StateContext(JobId, jobMethod);

            var transactionMock = new Mock<IWriteOnlyTransaction>();
            transactionMock.Setup(x => x.Jobs).Returns(_jobsMock.Object);
            transactionMock.Setup(x => x.Counters).Returns(_countersMock.Object);

            _context = new StateApplyingContext(stateContext, transactionMock.Object);
        }

        [Fact]
        public void ShouldWorkOnlyWithSucceededState()
        {
            var handler = new SucceededState.Handler();
            Assert.Equal(SucceededState.Name, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldSet_JobExpirationDate()
        {
            var handler = new SucceededState.Handler();
            handler.Apply(_context, null);

            _jobsMock.Verify(x => x.Expire(JobId, It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void Apply_ShouldIncrease_SucceededCounter()
        {
            var handler = new SucceededState.Handler();
            handler.Apply(_context, null);

            _countersMock.Verify(x => x.Increment("stats:succeeded"), Times.Once);
        }

        [Fact]
        public void Unapply_ShouldRemoveJobExpirationDate()
        {
            var handler = new SucceededState.Handler();
            handler.Unapply(_context);

            _jobsMock.Verify(x => x.Persist(JobId));
        }

        [Fact]
        public void Unapply_ShouldDecrementStatistics()
        {
            var handler = new SucceededState.Handler();
            handler.Unapply(_context);

            _countersMock.Verify(x => x.Decrement("stats:succeeded"), Times.Once);
        }

        public void TestMethod()
        {
        }
    }
}