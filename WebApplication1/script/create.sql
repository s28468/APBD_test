create table Project (
    IdProject int primary key,
    Name nvarchar(100),
    Deadline date
)

create table TaskType (
    IdTaskType int primary key, 
    Name nvarchar(100)
)

create table TeamMember(
    IdTeamMember int primary key,
    FirstName nvarchar(100),
    LastName nvarchar(100),
    Email nvarchar(100)
)

create table Task(
    IdTask int primary key,
    Name nvarchar(100),
    Description nvarchar(100),
    Deadline date, 
    IdProject int,
    IdTaskType int,
    IdAssignedTo int,
    IdCreator int,
    FOREIGN KEY (IdProject) references Project(IdProject),
    FOREIGN KEY (IdTaskType) references TaskType(IdTaskType),
    FOREIGN KEY (IdAssignedTo) references TeamMember(IdTeamMember),
    FOREIGN KEY (IdCreator) references TeamMember(IdTeamMember),
)