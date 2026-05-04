drop database if exists Bytesize;  -- FOR TESTING

create database if not exists Bytesize;
use Bytesize;

create table if not exists Preferences(
	-- keys
	PreferencesID int not null auto_increment,
    -- fields
    DarkMode bool not null default false,
    PrivateMode bool not null default true,
    -- constraints
    primary key (PreferencesID)
);

create table if not exists Statistics(
	-- keys
    StatisticsID int not null auto_increment,
    -- fields
    Experience int not null default 0,
    LessonCount int not null default 0,
    CourseCount int not null default 0,
    JoinDate date not null default (current_date),
    -- constraints
    primary key (StatisticsID)
);

create table if not exists SecurityInformation(
	-- keys
    SecurityInformationID int not null auto_increment,
    -- fields
    PasswordSalt varchar(255) not null,
    PasswordHash varchar(255) not null,
    EmailVerified bool not null default false,
    -- constraints
    primary key (SecurityInformationID)
);

create table if not exists UserInformation(
	-- keys
    UserInformationID int not null auto_increment,
    -- fields
    Username varchar(20) not null,
    ProfilePictureURL varchar(255) not null default '',
    FirstName varchar(20) not null default '',
    LastName varchar(20) not null default '',
    EmailAddress varchar(50) not null,
    DateOfBirth date, -- nullable since I don't think it's a good idea to have this mandatory for users to provide
    -- constraints
    primary key (UserInformationID)
);

create table if not exists Topic(
	-- keys
	TopicID int not null auto_increment,
    -- fields
    TopicName varchar(50) not null,
    -- constraints
    primary key (TopicID)
);

create table if not exists User(
	-- keys
	UserID int not null auto_increment,
    PreferencesID int not null,
    StatisticsID int not null,
    SecurityInformationID int not null,
    UserInformationID int not null,
	-- no fields in the interest of normalisation
    -- constraints
    primary key (UserID),
    foreign key (PreferencesID) references Preferences(PreferencesID),
    foreign key (StatisticsID) references Statistics(StatisticsID),
	foreign key (SecurityInformationID) references SecurityInformation(SecurityInformationID),
    foreign key (UserInformationID) references UserInformation(UserInformationID)
);

create table if not exists TopicProgress(
	-- keys
	TopicProgressID int not null auto_increment,
    TopicID int not null,
    -- fields
    QuestionsAnswered int not null default 0,
    QuestionsCorrect int not null default 0,
    -- QuestionsIncorrect will be derived
    -- constraints
    primary key (TopicProgressID),
    foreign key (TopicID) references Topic(TopicID)
);

create table if not exists Session(
	-- keys
    SessionToken varchar(16) not null,
    UserID int not null,
    -- fields
    LastUsed datetime not null default (current_timestamp),
    -- constraints
    primary key (SessionToken),
	foreign key (UserID) references User(UserID)
);